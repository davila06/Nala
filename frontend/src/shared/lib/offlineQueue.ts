import { openDB, type DBSchema, type IDBPDatabase } from 'idb'

// ── Types ─────────────────────────────────────────────────────────────────────

export type QueuedReportStatus = 'pending' | 'retrying' | 'conflict' | 'done'

/**
 * A lost-pet report captured while the device was offline.
 * Stored in IndexedDB so it survives page reloads and app restarts.
 */
export interface QueuedReport {
  /** Client-assigned UUID — NOT the server lostEventId. */
  id: string
  petId: string
  petName: string
  /** ISO — when the user submitted the form; NOT when the report was sent. */
  capturedAt: string
  lastSeenAt: string
  lastSeenLat: number | null
  lastSeenLng: number | null
  description: string | null
  publicMessage: string | null
  contactName: string | null
  contactPhone: string | null
  /**
   * Photo stored as a Blob (IndexedDB handles binary natively).
   * Converted back to File<FormData just before sending.
   */
  photoBlob: Blob | null
  status: QueuedReportStatus
  /** How many send attempts have been made. 0 = never tried. */
  retryCount: number
  /**
   * Epoch ms — when the next retry is allowed.
   * 0 means "retry immediately when online".
   */
  nextRetryAt: number
  /** Populated when status = 'done': the server-assigned lostEventId. */
  serverLostEventId: string | null
}

// ── IDB schema ────────────────────────────────────────────────────────────────

interface PawTrackOfflineDB extends DBSchema {
  'report-queue': {
    key: string
    value: QueuedReport
    indexes: {
      'by-status': string
      'by-pet': string
    }
  }
}

// ── DB singleton ──────────────────────────────────────────────────────────────

let _db: Promise<IDBPDatabase<PawTrackOfflineDB>> | null = null

function getDb(): Promise<IDBPDatabase<PawTrackOfflineDB>> {
  if (_db === null) {
    _db = openDB<PawTrackOfflineDB>('pawtrack-offline', 1, {
      upgrade(db) {
        const store = db.createObjectStore('report-queue', { keyPath: 'id' })
        store.createIndex('by-status', 'status')
        store.createIndex('by-pet', 'petId')
      },
    })
  }
  return _db
}

// ── CRUD ──────────────────────────────────────────────────────────────────────

/** Adding a report to the queue. Status starts as 'pending', no retries yet. */
export async function addQueuedReport(
  payload: Pick<
    QueuedReport,
    | 'id'
    | 'petId'
    | 'petName'
    | 'capturedAt'
    | 'lastSeenAt'
    | 'lastSeenLat'
    | 'lastSeenLng'
    | 'description'
    | 'publicMessage'
    | 'contactName'
    | 'contactPhone'
    | 'photoBlob'
  >,
): Promise<void> {
  const db = await getDb()
  const record: QueuedReport = {
    ...payload,
    status: 'pending',
    retryCount: 0,
    nextRetryAt: 0, // 0 = retry immediately on next opportunity
    serverLostEventId: null,
  }
  await db.put('report-queue', record)
}

/** All records regardless of status. */
export async function getAllQueuedReports(): Promise<QueuedReport[]> {
  const db = await getDb()
  return db.getAll('report-queue')
}

/** Records that need to be (re)sent: status is 'pending' or 'retrying'. */
export async function getActiveQueuedReports(): Promise<QueuedReport[]> {
  const db = await getDb()
  const all = await db.getAll('report-queue')
  return all.filter((r) => r.status === 'pending' || r.status === 'retrying')
}

/** Overwrite a record in place (used by the processor to update status / retry fields). */
export async function upsertQueuedReport(report: QueuedReport): Promise<void> {
  const db = await getDb()
  await db.put('report-queue', report)
}

/** Mark a queued record as successfully delivered. */
export async function markQueuedReportDone(id: string, serverLostEventId: string): Promise<void> {
  const db = await getDb()
  const record = await db.get('report-queue', id)
  if (!record) return
  await db.put('report-queue', { ...record, status: 'done', serverLostEventId })
}

/** Mark a queued record as conflicted (pet already has an active report on server). */
export async function markQueuedReportConflict(id: string): Promise<void> {
  const db = await getDb()
  const record = await db.get('report-queue', id)
  if (!record) return
  await db.put('report-queue', { ...record, status: 'conflict' })
}

/** Permanently remove a record from the queue (user dismissed it). */
export async function removeQueuedReport(id: string): Promise<void> {
  const db = await getDb()
  await db.delete('report-queue', id)
}

// ── Backoff calculator ────────────────────────────────────────────────────────

const BACKOFF_SCHEDULE_MS = [0, 30_000, 60_000, 120_000, 300_000, 600_000, 1_800_000] as const

/**
 * Returns the wait time in milliseconds before the next retry attempt.
 * Indexed by retryCount: 0 = immediate, scales up to 30 min.
 */
export function nextBackoffMs(retryCount: number): number {
  return BACKOFF_SCHEDULE_MS[Math.min(retryCount, BACKOFF_SCHEDULE_MS.length - 1)] ?? 1_800_000
}
