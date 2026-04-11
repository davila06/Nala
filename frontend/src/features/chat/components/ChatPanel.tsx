import { useEffect, useRef, useState } from 'react'
import { useChatMessages, useSendChatMessage } from '../hooks/useChatThread'
import type { ChatMessage } from '../api/chatApi'
import { Alert } from '@/shared/ui/Alert'

// ── Message bubble ────────────────────────────────────────────────────────────

function MessageBubble({ msg }: { msg: ChatMessage }) {
  const time = new Date(msg.sentAt).toLocaleTimeString('es-CR', {
    hour: '2-digit',
    minute: '2-digit',
  })

  return (
    <div className={`flex ${msg.isFromMe ? 'justify-end' : 'justify-start'} mb-2`}>
      <div
        className={`max-w-[75%] rounded-2xl px-4 py-2 text-sm ${
          msg.isFromMe
            ? 'rounded-br-sm bg-sand-900 text-white'
            : 'rounded-bl-sm bg-sand-100 text-sand-800'
        }`}
      >
        <p className="whitespace-pre-wrap break-words">{msg.body}</p>
        <p className={`mt-1 text-right text-[10px] ${msg.isFromMe ? 'text-sand-400' : 'text-sand-400'}`}>
          {time}
          {msg.isFromMe && (
            <span className="ml-1">{msg.isReadByRecipient ? '✓✓' : '✓'}</span>
          )}
        </p>
      </div>
    </div>
  )
}

// ── Chat panel ────────────────────────────────────────────────────────────────

interface ChatPanelProps {
  threadId: string
  lostPetEventId: string
  otherPartyName: string
}

export function ChatPanel({ threadId, lostPetEventId, otherPartyName }: ChatPanelProps) {
  const [text, setText] = useState('')
  const [sendError, setSendError] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  const { data: messages = [], isFetching } = useChatMessages(threadId)
  const { mutateAsync: sendMessage, isPending } = useSendChatMessage(threadId, lostPetEventId)

  // Scroll to bottom when new messages arrive.
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages.length])

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault()
    const body = text.trim()
    if (!body) return
    setSendError(null)

    try {
      await sendMessage({ body })
      setText('')
    } catch (err: unknown) {
      const msg =
        err instanceof Error
          ? err.message
          : 'No se pudo enviar el mensaje. Intenta de nuevo.'
      setSendError(msg)
    }
  }

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center gap-3 border-b border-sand-200 px-4 py-3">
        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-sand-900 text-xs font-bold text-white">
          {otherPartyName[0]?.toUpperCase() ?? '?'}
        </div>
        <div>
          <p className="text-sm font-semibold text-sand-900">{otherPartyName}</p>
          <p className="text-[10px] text-sand-400">
            Chat cifrado · sin compartir datos personales
            {isFetching && ' · actualizando…'}
          </p>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-4 py-4">
        {messages.length === 0 && (
          <p className="text-center text-sm text-sand-400">
            Inicia la conversación de forma segura.
          </p>
        )}
        {messages.map((m) => (
          <MessageBubble key={m.messageId} msg={m} />
        ))}
        <div ref={bottomRef} />
      </div>

      {/* Privacy reminder */}
      <div className="mx-4 mb-1 rounded-lg bg-brand-50 px-3 py-2 text-[11px] text-brand-700">
        Por seguridad, no compartas tu número de teléfono ni correo en el chat.
      </div>

      {/* Error */}
      {sendError && (
        <Alert variant="error" className="mx-4 mb-1">{sendError}</Alert>
      )}

      {/* Input */}
      <form onSubmit={handleSend} className="flex gap-2 border-t border-sand-200 px-4 py-3">
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault()
              void handleSend(e as unknown as React.FormEvent)
            }
          }}
          placeholder="Escribe un mensaje…"
          rows={2}
          maxLength={800}
          className="flex-1 resize-none rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none transition focus:border-sand-400"
        />
        <button
          type="submit"
          disabled={isPending || !text.trim()}
          className="flex h-10 w-10 shrink-0 items-center justify-center self-end rounded-xl bg-sand-900 text-white transition hover:bg-sand-700 disabled:opacity-40"
          aria-label="Enviar"
        >
          {isPending ? (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
          ) : (
            '↑'
          )}
        </button>
      </form>
    </div>
  )
}

