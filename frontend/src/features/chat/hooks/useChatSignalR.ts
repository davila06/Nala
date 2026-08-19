import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/features/auth/store/authStore";

const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

/**
 * Connects to /hubs/chat, joins the given threadId group, and invalidates
 * the messages cache on any NewMessage push — removing the need to poll.
 * Falls back gracefully: if the connection fails, the 10-second poll continues.
 */
export function useChatSignalR(threadId: string | null) {
  const qc = useQueryClient();
  const { accessToken } = useAuthStore();
  const connectionRef = useRef<signalR.HubConnection | null>(null);

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

    connection
      .start()
      .then(() => connection.invoke("JoinThread", threadId))
      .catch(() => {
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
    };
  }, [threadId, accessToken, qc]);
}
