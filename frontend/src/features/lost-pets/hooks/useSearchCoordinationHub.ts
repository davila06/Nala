import * as signalR from '@microsoft/signalr'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuthStore } from '@/features/auth/store/authStore'
import type { SearchZone } from '../api/searchCoordinationApi'

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export interface VolunteerLocation {
  userId: string
  lat: number
  lng: number
}

interface UseSearchCoordinationHubOptions {
  lostEventId: string
  onZoneClaimed?: (zone: SearchZone) => void
  onZoneCleared?: (zone: SearchZone) => void
  onZoneReleased?: (zone: SearchZone) => void
  onLocationUpdated?: (location: VolunteerLocation) => void
}

export function useSearchCoordinationHub({
  lostEventId,
  onZoneClaimed,
  onZoneCleared,
  onZoneReleased,
  onLocationUpdated,
}: UseSearchCoordinationHubOptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    const token = useAuthStore.getState().accessToken

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/search-coordination`, {
        accessTokenFactory: () => token ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('ZoneClaimed',   (zone: SearchZone) => onZoneClaimed?.(zone))
    connection.on('ZoneCleared',   (zone: SearchZone) => onZoneCleared?.(zone))
    connection.on('ZoneReleased',  (zone: SearchZone) => onZoneReleased?.(zone))
    connection.on('LocationUpdated', (loc: VolunteerLocation) => onLocationUpdated?.(loc))

    connection
      .start()
      .then(async () => {
        setIsConnected(true)
        await connection.invoke('JoinSearch', lostEventId)
      })
      .catch(() => setIsConnected(false))

    connection.onreconnected(async () => {
      setIsConnected(true)
      await connection.invoke('JoinSearch', lostEventId)
    })

    connection.onclose(() => setIsConnected(false))

    connectionRef.current = connection

    return () => {
      connection
        .invoke('LeaveSearch', lostEventId)
        .catch(() => {})
        .finally(() => connection.stop())
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lostEventId])

  const claimZone = useCallback(async (zoneId: string) => {
    await connectionRef.current?.invoke('ClaimZone', lostEventId, zoneId)
  }, [lostEventId])

  const clearZone = useCallback(async (zoneId: string) => {
    await connectionRef.current?.invoke('ClearZone', lostEventId, zoneId)
  }, [lostEventId])

  const releaseZone = useCallback(async (zoneId: string) => {
    await connectionRef.current?.invoke('ReleaseZone', lostEventId, zoneId)
  }, [lostEventId])

  const updateLocation = useCallback(async (lat: number, lng: number) => {
    await connectionRef.current?.invoke('UpdateLocation', lostEventId, lat, lng)
  }, [lostEventId])

  return { isConnected, claimZone, clearZone, releaseZone, updateLocation }
}
