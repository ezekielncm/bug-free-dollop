'use client'

import { useAuthStore } from '@/lib/store'

export default function SettingsPage() {
  const { user } = useAuthStore()

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
      <div className="bg-white rounded-2xl shadow p-6 space-y-4 max-w-lg">
        <h2 className="text-lg font-semibold text-gray-800">Profile</h2>
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p className="text-gray-500">Username</p>
            <p className="font-medium">{user?.username}</p>
          </div>
          <div>
            <p className="text-gray-500">Email</p>
            <p className="font-medium">{user?.email}</p>
          </div>
          <div>
            <p className="text-gray-500">Role</p>
            <p className="font-medium">{user?.role}</p>
          </div>
          <div>
            <p className="text-gray-500">Account Status</p>
            <p className={`font-medium ${user?.isActive ? 'text-green-600' : 'text-red-600'}`}>
              {user?.isActive ? 'Active' : 'Inactive'}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
