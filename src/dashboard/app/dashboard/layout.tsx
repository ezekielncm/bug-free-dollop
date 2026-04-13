'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/lib/store'

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const { isAuthenticated } = useAuthStore()

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/auth/login')
    }
  }, [isAuthenticated, router])

  if (!isAuthenticated()) return null

  return (
    <div className="flex h-screen bg-gray-100">
      {/* Sidebar */}
      <aside className="w-64 bg-white shadow-md flex flex-col">
        <div className="px-6 py-4 border-b">
          <h1 className="text-xl font-bold text-primary-700">MyApp</h1>
          <p className="text-xs text-gray-500">Dashboard</p>
        </div>
        <nav className="flex-1 px-4 py-6 space-y-2">
          <NavItem href="/dashboard" label="Overview" />
          <NavItem href="/dashboard/users" label="Users" />
          <NavItem href="/dashboard/settings" label="Settings" />
        </nav>
        <div className="px-4 py-4 border-t">
          <LogoutButton />
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto p-6">{children}</main>
    </div>
  )
}

function NavItem({ href, label }: { href: string; label: string }) {
  return (
    <a
      href={href}
      className="flex items-center px-3 py-2 text-sm font-medium text-gray-700 rounded-lg hover:bg-gray-100 hover:text-primary-600 transition"
    >
      {label}
    </a>
  )
}

function LogoutButton() {
  const logout = useAuthStore((s) => s.logout)
  const router = useRouter()

  return (
    <button
      onClick={() => { logout(); router.push('/auth/login') }}
      className="w-full text-left px-3 py-2 text-sm font-medium text-red-600 rounded-lg hover:bg-red-50 transition"
    >
      Sign Out
    </button>
  )
}
