import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { login as keycloakLogin, toSession } from '../api/auth'
import { clearToken, onTokenChange, readToken, writeToken } from './tokenStore'
import type { Role, Session } from '../types'

interface AuthValue {
  session: Session | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => void
  /** Verilen rollerden en az birine sahip mi? */
  hasRole: (...roles: Role[]) => boolean
}

const AuthContext = createContext<AuthValue | null>(null)

function readStoredSession(): Session | null {
  const token = readToken()
  if (!token) return null

  try {
    const session = toSession(token)
    // Süresi dolmuş token'ı hiç yükleme
    if (session.expiresAt <= Date.now()) {
      clearToken()
      return null
    }
    return session
  } catch {
    clearToken()
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(readStoredSession)

  const signOut = useCallback(() => {
    clearToken()
    setSession(null)
  }, [])

  const signIn = useCallback(async (email: string, password: string) => {
    const next = await keycloakLogin(email, password)
    writeToken(next.token)
    setSession(next)
  }, [])

  // Token store dışarıdan değişirse (ör. http katmanı 401'de temizlerse)
  // React state'ini senkronda tut.
  useEffect(() => onTokenChange(() => setSession(readStoredSession())), [])

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
