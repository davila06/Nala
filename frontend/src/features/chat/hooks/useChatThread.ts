import { useMemo, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  chatApi,
  type OpenThreadPayload,
  type SendMessagePayload,
} from "../api/chatApi";

// ── Query keys ─────────────────────────────────────────────────────────────────

const keys = {
  threads: (lostPetEventId: string) =>
    ["chat", "threads", lostPetEventId] as const,
  thread: (threadId: string) => ["chat", "thread", threadId] as const,
  messages: (threadId: string) => ["chat", "messages", threadId] as const,
  typing: (threadId: string) => ["chat", "typing", threadId] as const,
};

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useChatThreadById(threadId: string) {
  return useQuery({
    queryKey: keys.thread(threadId),
    queryFn: () => chatApi.getThreadById(threadId),
    enabled: !!threadId,
    staleTime: 30_000,
  });
}

export function useChatThreads(lostPetEventId: string, enabled = true) {
  return useQuery({
    queryKey: keys.threads(lostPetEventId),
    queryFn: () => chatApi.getThreads(lostPetEventId),
    enabled: enabled && !!lostPetEventId,
    refetchInterval: 15_000, // poll every 15 s for new threads
    staleTime: 10_000,
  });
}

export function useChatMessages(threadId: string, enabled = true) {
  return useQuery({
    queryKey: keys.messages(threadId),
    queryFn: () => chatApi.getMessages(threadId),
    enabled: enabled && !!threadId,
    // SignalR push via useChatSignalR mounted at page level; poll stays as fallback
    refetchInterval: 10_000,
    staleTime: 5_000,
  });
}

export function useOpenChatThread() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: OpenThreadPayload) => chatApi.openThread(payload),
    onSuccess: (_, variables) => {
      void queryClient.invalidateQueries({
        queryKey: keys.threads(variables.lostPetEventId),
      });
    },
  });
}

export function useSendChatMessage(threadId: string, lostPetEventId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SendMessagePayload) =>
      chatApi.sendMessage(threadId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: keys.messages(threadId) });
      void queryClient.invalidateQueries({
        queryKey: keys.threads(lostPetEventId),
      });
    },
  });
}

/** Polls the typing state of the other participant every 3 s. */
export function useOtherPartyTyping(threadId: string) {
  return useQuery({
    queryKey: keys.typing(threadId),
    queryFn: () => chatApi.getTypingState(threadId),
    enabled: !!threadId,
    refetchInterval: 3_000,
    staleTime: 0,
    select: (data) => data.isTyping,
  });
}

/** Returns a debounced typing notifier — safe to call on every keystroke. */
export function useNotifyTyping(threadId: string) {
  const mutation = useMutation({
    mutationFn: () => chatApi.notifyTyping(threadId),
  });
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const notify = useMemo(
    () => () => {
      if (timerRef.current) clearTimeout(timerRef.current);
      timerRef.current = setTimeout(() => mutation.mutate(), 500);
    },
    [mutation],
  );
  return notify;
}
