import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'

export interface RealtimeNotification {
  type: string
  message: string
  severity: 'Info' | 'Warning' | 'High' | 'Critical'
  occurredAt: string
}

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5083'
const HUB_URL = `${BASE_URL}/hubs/notifications`

export function useNotifications(maxHistory = 50) {
  const [notifications, setNotifications] = useState<RealtimeNotification[]>([])
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const token = localStorage.getItem('auth_token')
    if (!token) return

    let active = true
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.None)
      .build()

    connection.on('Notification', (notification: RealtimeNotification) => {
      if (active) setNotifications(prev => [notification, ...prev].slice(0, maxHistory))
    })

    // If StrictMode cleanup fires before start() resolves, stop immediately on connect.
    connection.start()
      .then(() => { if (!active) connection.stop() })
      .catch(() => {})
    connectionRef.current = connection

    return () => {
      active = false
      connection.stop().catch(() => {})
    }
  }, [maxHistory])

  const dismiss = (index: number) =>
    setNotifications(prev => prev.filter((_, i) => i !== index))

  return { notifications, dismiss }
}
