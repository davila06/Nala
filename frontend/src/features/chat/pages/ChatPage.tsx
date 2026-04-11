import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useChatMessages, useChatThreads, useOpenChatThread } from '../hooks/useChatThread'
import { ChatPanel } from '../components/ChatPanel'
import { useAuthStore } from '@/features/auth/store/authStore'
import { Button } from '@/shared/ui'
import { Alert } from '@/shared/ui/Alert'

/**
 * Full-page chat view.
 * Route params: `lostPetEventId` + `ownerUserId`.
 * When a finder lands here, a thread is opened automatically if none exists.
 * When an owner lands here with `threadId` in the URL, they go straight to that thread.
 */
export default function ChatPage() {
  const { lostPetEventId, ownerUserId, threadId: threadIdParam } = useParams<{
    lostPetEventId: string
    ownerUserId: string
    threadId?: string
  }>()
  const navigate = useNavigate()
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const currentUserId = useAuthStore((s) => s.user?.id)

  const [activeThreadId, setActiveThreadId] = useState<string | null>(threadIdParam ?? null)
  const [openError, setOpenError] = useState<string | null>(null)

  const { mutateAsync: openThread, isPending: isOpening } = useOpenChatThread()

  // For owner: load all threads for this event.
  const isOwner = currentUserId === ownerUserId
  const { data: threads = [], isLoading: threadsLoading } = useChatThreads(
    lostPetEventId ?? '',
    isOwner && !!lostPetEventId,
  )

  // For finder: auto-open a thread on mount (idempotent).
  const handleOpenThread = async () => {
    if (!lostPetEventId || !ownerUserId) return
    setOpenError(null)
    try {
      const res = await openThread({ lostPetEventId, ownerUserId })
      setActiveThreadId(res.threadId)
    } catch {
      setOpenError('No se pudo iniciar el chat. Intenta de nuevo.')
    }
  }

  // Prefetch messages for the active thread (ChatPanel also fetches internally).
  useChatMessages(activeThreadId ?? '', !!activeThreadId)

  const activeThread = threads.find((t) => t.threadId === activeThreadId)

  if (!isAuthenticated) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-6 text-center">
        <p className="text-4xl">💬</p>
        <h1 className="text-xl font-bold text-sand-900">Chat seguro</h1>
        <p className="text-sm text-sand-500">Debes iniciar sesión para usar el chat.</p>
        <Button onClick={() => navigate('/login')}>
          Iniciar sesión
        </Button>
      </div>
    )
  }

  return (
    <div className="flex h-dvh flex-col bg-white pb-[env(safe-area-inset-bottom,0px)]">
      {/* Topbar */}
      <div className="flex items-center gap-3 border-b border-sand-200 bg-white px-4 py-3">
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="text-sm text-sand-500 hover:text-sand-800"
        >
          ← Volver
        </button>
        <h1 className="text-sm font-bold text-sand-900">Chat seguro · PawTrack</h1>
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar: thread list (owner view) */}
        {isOwner && (
          <aside className="w-64 shrink-0 overflow-y-auto border-r border-sand-100 bg-sand-50">
            <p className="px-4 py-3 text-xs font-semibold uppercase tracking-widest text-sand-500">
              Conversaciones
            </p>
            {threadsLoading && (
              <div className="space-y-2 px-4">
                {[0, 1, 2].map((i) => (
                  <div key={i} className="h-14 animate-pulse rounded-xl bg-sand-200" />
                ))}
              </div>
            )}
            {!threadsLoading && threads.length === 0 && (
              <p className="px-4 py-2 text-xs text-sand-400">Sin mensajes aún.</p>
            )}
            {threads.map((t) => (
              <button
                key={t.threadId}
                type="button"
                onClick={() => setActiveThreadId(t.threadId)}
                className={`flex w-full flex-col px-4 py-3 text-left transition hover:bg-sand-100 ${
                  activeThreadId === t.threadId ? 'bg-sand-100' : ''
                }`}
              >
                <div className="flex items-center justify-between">
                  <span className="text-sm font-semibold text-sand-900">{t.otherPartyName}</span>
                  {t.unreadCount > 0 && (
                    <span className="rounded-full bg-sand-900 px-1.5 py-0.5 text-[10px] font-bold text-white">
                      {t.unreadCount}
                    </span>
                  )}
                </div>
                <span className="text-xs text-sand-400">
                  {new Date(t.lastMessageAt).toLocaleDateString('es-CR')}
                </span>
              </button>
            ))}
          </aside>
        )}

        {/* Main chat area */}
        <main className="flex flex-1 flex-col">
          {activeThreadId ? (
            <ChatPanel
              threadId={activeThreadId}
              lostPetEventId={lostPetEventId ?? ''}
              otherPartyName={activeThread?.otherPartyName ?? 'Participante'}
            />
          ) : (
            <div className="flex flex-1 flex-col items-center justify-center gap-4 px-6 text-center">
              <p className="text-4xl">💬</p>
              <h2 className="text-lg font-bold text-sand-900">Chat con el dueño</h2>
              <p className="max-w-xs text-sm text-sand-500">
                Tu número de teléfono y correo nunca se comparten. La conversación es
                completamente anónima para ambas partes.
              </p>
              {openError && (
                <Alert variant="error">{openError}</Alert>
              )}
              {!isOwner && (
                <button
                  type="button"
                  onClick={() => void handleOpenThread()}
                  disabled={isOpening}
                  className="flex items-center gap-2 rounded-full bg-sand-900 px-5 py-2.5 text-sm font-bold text-white disabled:opacity-50"
                >
                  {isOpening ? (
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  ) : null}
                  Iniciar conversación segura
                </button>
              )}
            </div>
          )}
        </main>
      </div>
    </div>
  )
}

