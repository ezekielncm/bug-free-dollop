'use client'

import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/lib/store'

const SIGNALR_URL = process.env.NEXT_PUBLIC_SIGNALR_URL ?? 'http://localhost:5000'

export function useSignalR() {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const { accessToken } = useAuthStore()

  const connect = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${SIGNALR_URL}/hubs/notifications`, {
        accessTokenFactory: () => accessToken ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    try {
      await connection.start()
      console.log('SignalR connected')
    } catch (err) {
      console.error('SignalR connection error:', err)
    }
  }, [accessToken])

  const disconnect = useCallback(async () => {
    await connectionRef.current?.stop()
  }, [])

  const on = useCallback(<T>(method: string, handler: (data: T) => void) => {
    connectionRef.current?.on(method, handler)
    return () => connectionRef.current?.off(method, handler)
  }, [])

  const invoke = useCallback(async (method: string, ...args: unknown[]) => {
    return connectionRef.current?.invoke(method, ...args)
  }, [])

  useEffect(() => {
    if (accessToken) {
      connect()
      return () => {
        disconnect()
      }
    }
  }, [accessToken, connect, disconnect])

  return { connect, disconnect, on, invoke, connection: connectionRef.current }
}
