import axios, { AxiosInstance, AxiosError } from 'axios'

// Read accessToken from Zustand's persisted storage key
function getStoredToken(): string | null {
  if (typeof window === 'undefined') return null
  try {
    const raw = localStorage.getItem('auth-storage')
    if (!raw) return null
    const parsed = JSON.parse(raw) as { state?: { accessToken?: string } }
    return parsed?.state?.accessToken ?? null
  } catch {
    return null
  }
}

function getStoredRefreshToken(): string | null {
  if (typeof window === 'undefined') return null
  try {
    const raw = localStorage.getItem('auth-storage')
    if (!raw) return null
    const parsed = JSON.parse(raw) as { state?: { refreshToken?: string } }
    return parsed?.state?.refreshToken ?? null
  } catch {
    return null
  }
}

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

export const apiClient: AxiosInstance = axios.create({
  baseURL: `${API_URL}/api`,
  headers: { 'Content-Type': 'application/json' },
})

// Request interceptor: attach token from Zustand persisted storage
apiClient.interceptors.request.use((config) => {
  const token = getStoredToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor: refresh token on 401
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean }
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      try {
        const refreshToken = getStoredRefreshToken()
        if (!refreshToken) throw new Error('No refresh token')

        const { data } = await axios.post(`${API_URL}/api/auth/refresh`, { refreshToken })
        // Update the Zustand persisted state with new tokens
        try {
          const raw = localStorage.getItem('auth-storage')
          if (raw) {
            const parsed = JSON.parse(raw) as { state: Record<string, unknown>; version?: number }
            parsed.state.accessToken = data.accessToken
            parsed.state.refreshToken = data.refreshToken
            localStorage.setItem('auth-storage', JSON.stringify(parsed))
          }
        } catch {
          // ignore parse errors
        }

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
        }
        return apiClient(originalRequest)
      } catch {
        // Clear persisted auth state on failed refresh
        localStorage.removeItem('auth-storage')
        window.location.href = '/auth/login'
      }
    }
    return Promise.reject(error)
  }
)

export default apiClient
