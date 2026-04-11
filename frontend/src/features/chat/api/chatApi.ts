import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type ChatThreadStatus = 'Active' | 'Closed' | 'Flagged'

export interface ChatThread {
  threadId: string
  lostPetEventId: string
  /** First name of the other participant, never surname or contact details. */
  otherPartyName: string
  status: ChatThreadStatus
  createdAt: string
  lastMessageAt: string
  unreadCount: number
}

export interface ChatMessage {
  messageId: string
  /** true = sent by the requesting user */
  isFromMe: boolean
  body: string
  sentAt: string
  isReadByRecipient: boolean
}

export interface OpenThreadPayload {
  lostPetEventId: string
  ownerUserId: string
}

export interface SendMessagePayload {
  body: string
}

// ── API client ─────────────────────────────────────────────────────────────────

export const chatApi = {
  openThread: (payload: OpenThreadPayload): Promise<{ threadId: string }> =>
    apiClient
      .post<{ threadId: string }>('/chat/threads', payload)
      .then((r) => r.data),

  getThreads: (lostPetEventId: string): Promise<ChatThread[]> =>
    apiClient
      .get<ChatThread[]>('/chat/threads', { params: { lostPetEventId } })
      .then((r) => r.data),

  getMessages: (threadId: string): Promise<ChatMessage[]> =>
    apiClient
      .get<ChatMessage[]>(`/chat/threads/${threadId}/messages`)
      .then((r) => r.data),

  sendMessage: (threadId: string, payload: SendMessagePayload): Promise<{ messageId: string }> =>
    apiClient
      .post<{ messageId: string }>(`/chat/threads/${threadId}/messages`, payload)
      .then((r) => r.data),
}
