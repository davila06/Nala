import { useEffect, useRef, useState, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  useChatMessages,
  useSendChatMessage,
  useOtherPartyTyping,
  useNotifyTyping,
} from "../hooks/useChatThread";
import type { ChatMessage } from "../api/chatApi";
import { Alert } from "@/shared/ui/Alert";
import { useHaptic } from "@/shared/hooks/useHaptic";

// ── Relative time formatter ───────────────────────────────────────────────────

function relativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 1) return "ahora";
  if (mins < 60) return `${mins}m`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h`;
  return new Date(dateStr).toLocaleDateString("es-CR", {
    day: "numeric",
    month: "short",
  });
}

// ── Message bubble ────────────────────────────────────────────────────────────

function MessageBubble({
  msg,
  isLatest,
}: {
  msg: ChatMessage;
  isLatest: boolean;
}) {
  const mine = msg.isFromMe;

  return (
    <motion.div
      layout
      initial={isLatest ? { opacity: 0, y: 10, scale: 0.96 } : false}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ duration: 0.2, ease: [0.4, 0, 0.2, 1] }}
      className={`flex mb-3 ${mine ? "justify-end" : "justify-start"}`}
    >
      {/* Avatar — other party */}
      {!mine && (
        <div className="mr-2 shrink-0 self-end">
          <div className="flex h-7 w-7 items-center justify-center rounded-full bg-sand-800 text-xs font-bold text-white">
            ?
          </div>
        </div>
      )}

      <div
        className={`flex max-w-[78%] flex-col ${mine ? "items-end" : "items-start"}`}
      >
        {/* Bubble */}
        <div
          className={[
            "relative rounded-2xl px-4 py-2.5 text-sm leading-relaxed shadow-sm",
            mine
              ? "rounded-br-sm bg-brand-500 text-white"
              : "rounded-bl-sm field-input border border-sand-200",
          ].join(" ")}
        >
          <p className="whitespace-pre-wrap wrap-break-word">{msg.body}</p>
        </div>

        {/* Meta — time + read receipt */}
        <div
          className={`mt-1 flex items-center gap-1 ${mine ? "flex-row-reverse" : ""}`}
        >
          <span className="text-[10px] text-sand-400">
            {relativeTime(msg.sentAt)}
          </span>
          {mine && (
            <span
              className={`text-[10px] ${msg.isReadByRecipient ? "text-brand-400" : "text-sand-300"}`}
            >
              {msg.isReadByRecipient ? "✓✓" : "✓"}
            </span>
          )}
        </div>
      </div>
    </motion.div>
  );
}

// ── Typing indicator ──────────────────────────────────────────────────────────

function TypingIndicator() {
  return (
    <div className="flex items-end gap-2 mb-3 justify-start">
      <div className="flex h-7 w-7 items-center justify-center rounded-full bg-sand-800 text-xs font-bold text-white shrink-0">
        ?
      </div>
      <div className="rounded-2xl rounded-bl-sm field-input border border-sand-200 px-4 py-3 shadow-sm">
        <div className="flex gap-1">
          {[0, 1, 2].map((i) => (
            <span
              key={i}
              className="h-1.5 w-1.5 rounded-full bg-sand-400 inline-block"
              style={{
                animation: `pulse-soft 1.2s ease-in-out ${i * 0.2}s infinite`,
              }}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

// ── Chat panel ────────────────────────────────────────────────────────────────

interface ChatPanelProps {
  threadId: string;
  lostPetEventId: string;
  otherPartyName: string;
}

export function ChatPanel({
  threadId,
  lostPetEventId,
  otherPartyName,
}: ChatPanelProps) {
  const [text, setText] = useState("");
  const [sendError, setSendError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);
  const typingTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const { tap, error: hapticError } = useHaptic();

  const { data: messages = [], isFetching } = useChatMessages(threadId);
  const { mutateAsync: sendMessage, isPending } = useSendChatMessage(
    threadId,
    lostPetEventId,
  );
  const { data: otherPartyIsTyping = false } = useOtherPartyTyping(threadId);
  const { mutate: notifyTyping } = useNotifyTyping(threadId);

  const handleTextChange = useCallback(
    (value: string) => {
      setText(value);
      notifyTyping();
      if (typingTimerRef.current) clearTimeout(typingTimerRef.current);
    },
    [notifyTyping],
  );

  // Scroll to bottom when new messages arrive.
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages.length]);

  // Haptic tap when new messages arrive from the other party
  const prevLengthRef = useRef(0);
  useEffect(() => {
    if (messages.length > prevLengthRef.current) {
      const newest = messages[messages.length - 1];
      if (newest && !newest.isMine) tap();
    }
    prevLengthRef.current = messages.length;
  }, [messages.length]);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    const body = text.trim();
    if (!body) return;
    setSendError(null);
    tap();

    try {
      await sendMessage({ body });
      setText("");
    } catch (err: unknown) {
      hapticError();
      const msg =
        err instanceof Error
          ? err.message
          : "No se pudo enviar el mensaje. Intenta de nuevo.";
      setSendError(msg);
    }
  };

  return (
    <div className="flex h-full flex-col bg-surface-warm">
      {/* Header */}
      <div className="flex items-center gap-3 border-b border-sand-200 field-input px-4 py-3 shadow-sm">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-sand-900 text-xs font-bold text-white ring-2 ring-sand-200">
          {otherPartyName[0]?.toUpperCase() ?? "?"}
        </div>
        <div>
          <p className="text-sm font-semibold text-sand-900">
            {otherPartyName}
          </p>
          <p className="text-[10px] text-sand-400">
            Chat cifrado · sin compartir datos personales
            {isFetching && " · actualizando…"}
          </p>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-4 py-4">
        {messages.length === 0 && !isPending && (
          <div className="flex flex-col items-center justify-center py-12 gap-3 text-center">
            <div className="text-4xl" aria-hidden="true">
              💬
            </div>
            <p className="text-sm text-sand-500 max-w-xs leading-relaxed">
              Esta conversación es cifrada y anónima. Coordina la entrega de
              forma segura.
            </p>
          </div>
        )}
        <AnimatePresence initial={false}>
          {messages.map((m, i) => (
            <MessageBubble
              key={m.messageId}
              msg={m}
              isLatest={i === messages.length - 1}
            />
          ))}
        </AnimatePresence>
        {/* Show typing indicator: isPending = current user sending; otherPartyIsTyping = real polling */}
        {(isPending || otherPartyIsTyping) && <TypingIndicator />}
        <div ref={bottomRef} />
      </div>

      {/* Privacy reminder */}
      <div className="mx-4 mb-2 rounded-xl bg-trust-50 border border-trust-100 px-3 py-2 text-[11px] text-trust-700 leading-snug">
        🔒 Por seguridad, no compartas tu número ni correo en el chat.
      </div>

      {/* Error */}
      {sendError && (
        <Alert variant="error" className="mx-4 mb-1">
          {sendError}
        </Alert>
      )}

      {/* Input */}
      <form
        onSubmit={handleSend}
        className="flex gap-2 border-t border-sand-200 px-4 py-3"
      >
        <textarea
          value={text}
          onChange={(e) => handleTextChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              void handleSend(e as unknown as React.FormEvent);
            }
          }}
          placeholder="Escribe un mensaje…"
          rows={2}
          maxLength={800}
          className="field-input flex-1 resize-none rounded-xl border border-sand-200 px-3 py-2 text-sm outline-none transition focus:border-sand-400"
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
            "↑"
          )}
        </button>
      </form>
    </div>
  );
}
