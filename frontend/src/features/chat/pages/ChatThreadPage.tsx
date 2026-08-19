import { useNavigate, useParams } from "react-router-dom";
import { ChatPanel } from "../components/ChatPanel";
import { useChatThreadById } from "../hooks/useChatThread";
import { useChatSignalR } from "../hooks/useChatSignalR";
import { Skeleton } from "@/shared/ui/Spinner";

/**
 * Standalone chat view accessed via a thread ID only.
 * Used when navigating from notifications (which only carry the threadId).
 * Route: /chat/t/:threadId
 */
export default function ChatThreadPage() {
  const { threadId } = useParams<{ threadId: string }>();
  const navigate = useNavigate();
  const {
    data: thread,
    isLoading,
    isError,
  } = useChatThreadById(threadId ?? "");

  // Real-time push via SignalR — poll stays as fallback
  const connectionState = useChatSignalR(threadId ?? null);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-lg space-y-3 px-4 py-10">
        <Skeleton className="h-12 rounded-xl" />
        <Skeleton className="h-64 rounded-2xl" />
        <Skeleton className="h-10 rounded-xl" />
      </div>
    );
  }

  if (isError || !thread || !threadId) {
    return (
      <div className="flex flex-col items-center justify-center py-20 gap-3 text-center px-6">
        <span className="text-4xl" aria-hidden="true">
          💬
        </span>
        <p className="text-sm text-sand-500">
          No se encontró esta conversación.
        </p>
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="text-sm text-brand-600 hover:underline"
        >
          ← Volver
        </button>
      </div>
    );
  }

  return (
    <div className="flex h-dvh flex-col pb-[env(safe-area-inset-bottom,0px)]">
      {/* Topbar */}
      <div className="flex items-center gap-3 border-b border-sand-200 px-4 py-3 bg-white dark:bg-sand-900">
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="text-sm text-sand-500 hover:text-sand-800"
        >
          ← Volver
        </button>
        <h1 className="text-sm font-bold text-sand-900 dark:text-sand-100">
          Chat seguro · {thread.otherPartyName}
        </h1>
      </div>

      <div className="flex-1 overflow-hidden">
        <ChatPanel
          threadId={threadId}
          lostPetEventId={thread.lostPetEventId}
          otherPartyName={thread.otherPartyName}
          connectionState={connectionState}
        />
      </div>
    </div>
  );
}
