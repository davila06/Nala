import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/features/auth/store/authStore";

const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

export type ChatConnectionState =
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

/**
 * Connects to /hubs/chat, joins the given threadId group, and invalidates
 * the messages cache on any NewMessage push — removing the need to poll.
 * Falls back gracefully: if the connection fails, the 10-second poll continues.
 * Returns the current connection state for optional UI indicators.
 */
export function useChatSignalR(threadId: string | null): ChatConnectionState {
  const qc = useQueryClient();
  const { accessToken } = useAuthStore();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [state, setState] = useState<ChatConnectionState>("disconnected");

  useEffect(() => {
    if (!threadId || !accessToken) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/chat`, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("NewMessage", () => {
      void qc.invalidateQueries({ queryKey: ["chat", "messages", threadId] });
    });

    connection.onreconnecting(() => setState("reconnecting"));
    connection.onreconnected(() => setState("connected"));
    connection.onclose(() => setState("disconnected"));

    setState("connecting");
    connection
      .start()
      .then(() => {
        setState("connected");
        return connection.invoke("JoinThread", threadId);
      })
      .catch(() => {
        setState("disconnected");
        /* silent — poll fallback handles messages */
      });

    connectionRef.current = connection;

    return () => {
      connection
        .invoke("LeaveThread", threadId)
        .catch(() => {
          /* ignore */
        })
        .finally(() => void connection.stop());
      connectionRef.current = null;
      setState("disconnected");
    };
  }, [threadId, accessToken, qc]);

  return state;
}
