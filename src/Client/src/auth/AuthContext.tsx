import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { login as keycloakLogin, toSession } from '../api/auth'
import { configureHttp } from '../api/http'
import type { Role, Session } from '../types'

const STORAGE_KEY = 'news-portal.token'

interface AuthValue {
  session: Session | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => void
  /** Verilen rollerden en az birine sahip mi? */
  hasRole: (...roles: Role[]) => boolean
}

const AuthContext = createContext<AuthValue | null>(null)

function readStoredSession(): Session | null {
  const token = localStorage.getItem(STORAGE_KEY)
  if (!token) return null

  try {
    const session = toSession(token)
    // Süresi dolmuş token'ı hiç yükleme
    if (session.expiresAt <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return session
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(readStoredSession)

  const signOut = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY)
    setSession(null)
  }, [])

  const signIn = useCallback(async (email: string, password: string) => {
    const next = await keycloakLogin(email, password)
    localStorage.setItem(STORAGE_KEY, next.token)
    setSession(next)
  }, [])

  // http katmanı token'ı ve 401 davranışını buradan alır
  useEffect(() => {
    configureHttp(() => session?.token ?? null, signOut)
  }, [session, signOut])

  // Token süresi dolduğunda oturumu kendiliğinden düşür
  useEffect(() => {
    if (!session) return
    const timeout = setTimeout(signOut, Math.max(0, session.expiresAt - Date.now()))
    return () => clearTimeout(timeout)
  }, [session, signOut])

  const value = useMemo<AuthValue>(
    () => ({
      session,
      signIn,
      signOut,
      hasRole: (...roles) => roles.some((role) => session?.roles.includes(role) ?? false),
    }),
    [session, signIn, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth, AuthProvider içinde kullanılmalı.')
  return context
}
