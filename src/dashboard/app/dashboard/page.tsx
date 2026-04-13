'use client'

import { useEffect, useState } from 'react'
import { useSignalR } from '@/hooks/useSignalR'
import { useAuthStore } from '@/lib/store'
import toast from 'react-hot-toast'

export default function DashboardPage() {
  const { user } = useAuthStore()
  const { on } = useSignalR()
  const [notifications, setNotifications] = useState<string[]>([])

  useEffect(() => {
    const unsubscribe = on<{ message: string; timestamp: string }>('Notification', (data) => {
      setNotifications((prev) => [`[${new Date(data.timestamp).toLocaleTimeString()}] ${data.message}`, ...prev].slice(0, 20))
      toast(data.message)
    })
    return unsubscribe
  }, [on])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Welcome back, {user?.firstName ?? user?.username}!
        </h1>
        <p className="text-gray-500 text-sm mt-1">Here&apos;s what&apos;s happening today.</p>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {[
          { label: 'Status', value: 'Online', color: 'text-green-600' },
          { label: 'Role', value: user?.role ?? 'User', color: 'text-primary-600' },
          { label: 'Real-time', value: 'Connected', color: 'text-purple-600' },
        ].map(({ label, value, color }) => (
          <div key={label} className="bg-white rounded-2xl shadow p-5">
            <p className="text-sm text-gray-500">{label}</p>
            <p className={`text-2xl font-semibold mt-1 ${color}`}>{value}</p>
          </div>
        ))}
      </div>

      {/* Live notifications */}
      <div className="bg-white rounded-2xl shadow p-5">
        <h2 className="text-lg font-semibold text-gray-800 mb-3">Live Notifications</h2>
        {notifications.length === 0 ? (
          <p className="text-gray-400 text-sm">No notifications yet. Waiting for real-time events…</p>
        ) : (
          <ul className="space-y-1">
            {notifications.map((n, i) => (
              <li key={i} className="text-sm text-gray-700 bg-gray-50 rounded px-3 py-1">{n}</li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
