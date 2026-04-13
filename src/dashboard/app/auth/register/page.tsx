'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import toast from 'react-hot-toast'
import { authService, userService } from '@/lib/services'
import { useAuthStore } from '@/lib/store'
import Link from 'next/link'

const registerSchema = z.object({
  username: z.string().min(3, 'Username must be at least 3 characters'),
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  firstName: z.string().optional(),
  lastName: z.string().optional(),
})

type RegisterFormData = z.infer<typeof registerSchema>

export default function RegisterPage() {
  const router = useRouter()
  const setAuth = useAuthStore((s) => s.setAuth)
  const [loading, setLoading] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  })

  const onSubmit = async (data: RegisterFormData) => {
    setLoading(true)
    try {
      const authData = await authService.register(data)
      const user = await userService.getMe()
      setAuth(user, authData.accessToken, authData.refreshToken)
      toast.success('Account created successfully!')
      router.push('/dashboard')
    } catch {
      toast.error('Registration failed. Email or username may already be taken.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="bg-white rounded-2xl shadow-lg p-8 w-full max-w-md">
        <h1 className="text-2xl font-bold text-gray-900 mb-6 text-center">Create Account</h1>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {[
            { name: 'username' as const, label: 'Username', type: 'text', placeholder: 'johndoe' },
            { name: 'email' as const, label: 'Email', type: 'email', placeholder: 'you@example.com' },
            { name: 'password' as const, label: 'Password', type: 'password', placeholder: '••••••••' },
            { name: 'firstName' as const, label: 'First Name (optional)', type: 'text', placeholder: 'John' },
            { name: 'lastName' as const, label: 'Last Name (optional)', type: 'text', placeholder: 'Doe' },
          ].map(({ name, label, type, placeholder }) => (
            <div key={name}>
              <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
              <input
                {...register(name)}
                type={type}
                placeholder={placeholder}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
              {errors[name] && <p className="text-red-500 text-xs mt-1">{errors[name]?.message}</p>}
            </div>
          ))}
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-primary-600 hover:bg-primary-700 text-white font-medium py-2 px-4 rounded-lg transition disabled:opacity-50"
          >
            {loading ? 'Creating account…' : 'Register'}
          </button>
        </form>
        <p className="text-center text-sm text-gray-600 mt-4">
          Already have an account?{' '}
          <Link href="/auth/login" className="text-primary-600 hover:underline">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
