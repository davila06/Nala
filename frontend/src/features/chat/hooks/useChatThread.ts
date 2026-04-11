import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { chatApi, type OpenThreadPayload, type SendMessagePayload } from '../api/chatApi'

// ── Query keys ─────────────────────────────────────────────────────────────────

const keys = {
  threads: (lostPetEventId: string) => ['chat', 'threads', lostPetEventId] as const,
  messages: (threadId: string) => ['chat', 'messages', threadId] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useChatThreads(lostPetEventId: string, enabled = true) {
  return useQuery({
    queryKey: keys.threads(lostPetEventId),
    queryFn: () => chatApi.getThreads(lostPetEventId),
    enabled: enabled && !!lostPetEventId,
    refetchInterval: 15_000, // poll every 15 s for new threads
    staleTime: 10_000,
  })
}

export function useChatMessages(threadId: string, enabled = true) {
  return useQuery({
    queryKey: keys.messages(threadId),
    queryFn: () => chatApi.getMessages(threadId),
    enabled: enabled && !!threadId,
    refetchInterval: 5_000, // poll every 5 s for new messages
    staleTime: 2_000,
  })
}

export function useOpenChatThread() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: OpenThreadPayload) => chatApi.openThread(payload),
    onSuccess: (_, variables) => {
      void queryClient.invalidateQueries({
        queryKey: keys.threads(variables.lostPetEventId),
      })
    },
  })
}

export function useSendChatMessage(threadId: string, lostPetEventId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: SendMessagePayload) => chatApi.sendMessage(threadId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: keys.messages(threadId) })
      void queryClient.invalidateQueries({ queryKey: keys.threads(lostPetEventId) })
    },
  })
}
