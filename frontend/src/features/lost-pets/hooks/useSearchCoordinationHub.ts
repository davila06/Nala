import * as signalR from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState } from "react";
import { useAuthStore } from "@/features/auth/store/authStore";
import type { SearchZone } from "../api/searchCoordinationApi";

const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

export interface VolunteerLocation {
  clientId: string;
  lat: number;
  lng: number;
  isPrecise: boolean;
  expiresAt: string;
}

export interface LocationSharingState {
  clientId: string;
  isSharing: boolean;
  isPrecise: boolean;
  expiresAt: string | null;
  recipientCount: number;
}

export interface LocationSharingRecipientsState {
  recipientCount: number;
  clientIds: string[];
}

interface UseSearchCoordinationHubOptions {
  lostEventId: string;
  onZoneClaimed?: (zone: SearchZone) => void;
  onZoneCleared?: (zone: SearchZone) => void;
  onZoneReleased?: (zone: SearchZone) => void;
  onLocationUpdated?: (location: VolunteerLocation) => void;
  onLocationSharingStateChanged?: (state: LocationSharingState) => void;
  onLocationSharingRecipientsChanged?: (state: LocationSharingRecipientsState) => void;
}

export function useSearchCoordinationHub({
  lostEventId,
  onZoneClaimed,
  onZoneCleared,
  onZoneReleased,
  onLocationUpdated,
  onLocationSharingStateChanged,
  onLocationSharingRecipientsChanged,
}: UseSearchCoordinationHubOptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const callbacksRef = useRef({
    onZoneClaimed,
    onZoneCleared,
    onZoneReleased,
    onLocationUpdated,
    onLocationSharingStateChanged,
    onLocationSharingRecipientsChanged,
  });
  callbacksRef.current = {
    onZoneClaimed,
    onZoneCleared,
    onZoneReleased,
    onLocationUpdated,
    onLocationSharingStateChanged,
    onLocationSharingRecipientsChanged,
  };
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const token = useAuthStore.getState().accessToken;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/search-coordination`, {
        accessTokenFactory: () => token ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("ZoneClaimed", (zone: SearchZone) => callbacksRef.current.onZoneClaimed?.(zone));
    connection.on("ZoneCleared", (zone: SearchZone) => callbacksRef.current.onZoneCleared?.(zone));
    connection.on("ZoneReleased", (zone: SearchZone) => callbacksRef.current.onZoneReleased?.(zone));
    connection.on("LocationUpdated", (loc: VolunteerLocation) => callbacksRef.current.onLocationUpdated?.(loc));
    connection.on("LocationSharingStateChanged", (state: LocationSharingState) =>
      callbacksRef.current.onLocationSharingStateChanged?.(state),
    );
    connection.on("LocationSharingRecipientsChanged", (state: LocationSharingRecipientsState) =>
      callbacksRef.current.onLocationSharingRecipientsChanged?.(state),
    );

    connection
      .start()
      .then(async () => {
        await connection.invoke("JoinSearch", lostEventId);
        setIsConnected(true);
      })
      .catch(() => setIsConnected(false));

    connection.onreconnected(() => {
      void (async () => {
        await connection.invoke("JoinSearch", lostEventId);
        setIsConnected(true);
      })();
    });

    connection.onclose(() => setIsConnected(false));

    connectionRef.current = connection;

    return () => {
      connection
        .invoke("LeaveSearch", lostEventId)
        .catch(() => {})
        .finally(() => connection.stop());
    };
  }, [lostEventId]);

  const claimZone = useCallback(
    async (zoneId: string) => {
      await connectionRef.current?.invoke("ClaimZone", lostEventId, zoneId);
    },
    [lostEventId],
  );

  const clearZone = useCallback(
    async (zoneId: string) => {
      await connectionRef.current?.invoke("ClearZone", lostEventId, zoneId);
    },
    [lostEventId],
  );

  const releaseZone = useCallback(
    async (zoneId: string) => {
      await connectionRef.current?.invoke("ReleaseZone", lostEventId, zoneId);
    },
    [lostEventId],
  );

  const updateLocation = useCallback(
    async (lat: number, lng: number) => {
      await connectionRef.current?.invoke("UpdateLocation", lostEventId, lat, lng);
    },
    [lostEventId],
  );

  const startLocationSharing = useCallback(
    async (precise = false) => {
      await connectionRef.current?.invoke("StartLocationSharing", lostEventId, precise);
    },
    [lostEventId],
  );

  const stopLocationSharing = useCallback(async () => {
    await connectionRef.current?.invoke("StopLocationSharing", lostEventId);
  }, [lostEventId]);

  return {
    isConnected,
    claimZone,
    clearZone,
    releaseZone,
    updateLocation,
    startLocationSharing,
    stopLocationSharing,
  };
}
