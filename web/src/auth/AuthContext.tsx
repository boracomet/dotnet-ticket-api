import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { api } from '../api/client'
import type { AuthResponse } from '../api/types'

const STORAGE_KEY = 'ticket-api-auth'

interface AuthState {
  accessToken: string
  userId: string
  email: string
  fullName: string
  role: string
}

interface AuthContextValue {
  user: AuthState | null
  token: string | null
  isAdmin: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, fullName: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function loadStored(): AuthState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as AuthState
  } catch {
    return null
  }
}

function toState(res: AuthResponse): AuthState {
  return {
    accessToken: res.accessToken,
    userId: res.userId,
    email: res.email,
    fullName: res.fullName,
    role: res.role,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState | null>(() => loadStored())

  const persist = useCallback((state: AuthState | null) => {
    setUser(state)
    if (state) localStorage.setItem(STORAGE_KEY, JSON.stringify(state))
    else localStorage.removeItem(STORAGE_KEY)
  }, [])

  const login = useCallback(
    async (email: string, password: string) => {
      persist(toState(await api.login(email, password)))
    },
    [persist],
  )

  const register = useCallback(
    async (email: string, fullName: string, password: string) => {
      persist(toState(await api.register(email, fullName, password)))
    },
    [persist],
  )

  const logout = useCallback(() => persist(null), [persist])

  const value = useMemo(
    () => ({
      user,
      token: user?.accessToken ?? null,
      isAdmin: (user?.role ?? '').toLowerCase() === 'admin',
      login,
      register,
      logout,
    }),
    [user, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
