import { useCallback, useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/lib/apiClient'
import {
  type QueuedReport,
  getAllQueuedReports,
  getActiveQueuedReports,
  markQueuedReportConflict,
  markQueuedReportDone,
  nextBackoffMs,
  removeQueuedReport,
  upsertQueuedReport,
} from '@/shared/lib/offlineQueue'

// ── Minimal type for conflict detection ───────────────────────────────────────

interface ActiveLostEventSlim {
  id: string
}

// ── Internal helpers ──────────────────────────────────────────────────────────

/**
 * Build FormData for the /api/lost-pets endpoint from a queued report.
 * Converts the stored Blob back to a File if present.
 */
function buildFormData(report: QueuedReport): FormData {
  const form = new FormData()
  form.append('petId', report.petId)
  form.append('lastSeenAt', report.lastSeenAt)
  if (report.lastSeenLat != null) form.append('lastSeenLat', String(report.lastSeenLat))
  if (report.lastSeenLng != null) form.append('lastSeenLng', String(report.lastSeenLng))
  if (report.description != null) form.append('description', report.description)
  if (report.publicMessage != null) form.append('publicMessage', report.publicMessage)
  if (report.contactName != null) form.append('contactName', report.contactName)
  if (report.contactPhone != null) form.append('contactPhone', report.contactPhone)
  if (report.photoBlob != null) {
    const mimeType = report.photoBlob.type || 'image/jpeg'
    const ext = mimeType.includes('png') ? 'png' : 'jpg'
    const file = new File([report.photoBlob], `photo.${ext}`, { type: mimeType })
    form.append('recentPhoto', file)
  }
  return form
}

// ── Hook return type ──────────────────────────────────────────────────────────

export interface OfflineQueueState {
  /** Items that are pending or retrying (waiting to be sent). */
  pendingItems: QueuedReport[]
  /** Items where a conflicting active report was found on the server. */
  conflictItems: QueuedReport[]
  /** Items successfully sent in this session. */
  doneSinceMount: QueuedReport[]
  isSyncing: boolean
  /** Trigger an immediate retry of all due items. */
  retryNow: () => void
  /** Remove a conflict or done item from the queue and dismiss from UI. */
  dismiss: (id: string) => Promise<void>
}

// ── Hook ──────────────────────────────────────────────────────────────────────

/**
 * Manages the offline report queue:
 * - Loads persisted items from IndexedDB on mount.
 * - Processes due items when the device comes online.
 * - Exponential backoff on failed send attempts.
 * - Conflict detection before each send attempt.
 * - Invalidates React Query caches on successful submission.
 *
 * Mount this hook ONCE at the authenticated layout root via OfflineQueueBanner.
 */
export function useOfflineReportQueue(): OfflineQueueState {
  const queryClient = useQueryClient()
  const [allItems, setAllItems] = useState<QueuedReport[]>([])
  const [isSyncing, setIsSyncing] = useState(false)
  const [doneSinceMount, setDoneSinceMount] = useState<QueuedReport[]>([])
  const isProcessingRef = useRef(false)

  const refreshItems = useCallback(async (): Promise<void> => {
    const items = await getAllQueuedReports()
    setAllItems(items)
  }, [])

  const processQueue = useCallback(async (): Promise<void> => {
    if (!navigator.onLine || isProcessingRef.current) return

    isProcessingRef.current = true
    setIsSyncing(true)

    try {
      const active = await getActiveQueuedReports()
      const now = Date.now()
      const due = active.filter((r) => r.nextRetryAt <= now)

      for (const report of due) {
        // Mark as 'retrying' so the UI shows in-progress state
        await upsertQueuedReport({ ...report, status: 'retrying' })
        await refreshItems()

        try {
          // ── Conflict detection ──────────────────────────────────────────
          // If the pet already has an active report on the server, there is
          // no point sending this queued entry — it would be rejected or
          // create a duplicate.
          const existing = await apiClient
            .get<ActiveLostEventSlim | null>(`/lost-pets/by-pet/${report.petId}`)
            .then((r) => r.data)

          if (existing !== null) {
            await markQueuedReportConflict(report.id)
            await refreshItems()
            continue
          }

          // ── Send ───────────────────────────────────────────────────────
          const form = buildFormData(report)
          const { data } = await apiClient.post<{ id: string }>('/lost-pets', form)
          const serverLostEventId = data.id

          await markQueuedReportDone(report.id, serverLostEventId)

          // Surface the successfully synced item to the UI for user feedback
          setDoneSinceMount((prev) => [
            ...prev,
            { ...report, status: 'done', serverLostEventId },
          ])

          // Invalidate all relevant caches so any mounted views refresh
          void queryClient.invalidateQueries({ queryKey: ['pets'] })
          void queryClient.invalidateQueries({ queryKey: ['pet', report.petId] })
          void queryClient.invalidateQueries({ queryKey: ['lost-pet', report.petId] })
        } catch {
          // Network error or server error — schedule a retry with exponential backoff
          const nextRetryCount = report.retryCount + 1
          await upsertQueuedReport({
            ...report,
            status: 'pending',
            retryCount: nextRetryCount,
            nextRetryAt: Date.now() + nextBackoffMs(nextRetryCount),
          })
        }

        await refreshItems()
      }
    } finally {
      await refreshItems()
      setIsSyncing(false)
      isProcessingRef.current = false
    }
  }, [queryClient, refreshItems])

  // Load persisted state on mount and attempt processing immediately if online
  useEffect(() => {
    void refreshItems().then(() => {
      if (navigator.onLine) void processQueue()
    })
  }, [refreshItems, processQueue])

  // Process queue whenever connectivity is restored
  useEffect(() => {
    const handler = (): void => { void processQueue() }
    window.addEventListener('online', handler)
    return () => window.removeEventListener('online', handler)
  }, [processQueue])

  // Periodic tick: retry items whose nextRetryAt has elapsed
  useEffect(() => {
    const interval = setInterval(() => {
      if (navigator.onLine) void processQueue()
    }, 30_000)
    return () => clearInterval(interval)
  }, [processQueue])

  const dismiss = useCallback(async (id: string): Promise<void> => {
    await removeQueuedReport(id)
    setDoneSinceMount((prev) => prev.filter((r) => r.id !== id))
    await refreshItems()
  }, [refreshItems])

  return {
    pendingItems: allItems.filter((r) => r.status === 'pending' || r.status === 'retrying'),
    conflictItems: allItems.filter((r) => r.status === 'conflict'),
    doneSinceMount,
    isSyncing,
    retryNow: () => { void processQueue() },
    dismiss,
  }
}
