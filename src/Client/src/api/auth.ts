import type { Role, Session, TokenResponse } from '../types'

const REALM = 'news-portal'
const CLIENT_ID = 'news-portal-client'

/**
 * Keycloak "direct access grant" (password grant) ile token alır.
 *
 * Not: Parolanın SPA üzerinden geçmesi üretim için ideal değil; doğrusu
 * authorization code + PKCE yönlendirme akışıdır. Realm'de her iki akış da
 * açık; burada kurulum basitliği için direct grant tercih edildi.
 */
export async function login(username: string, password: string): Promise<Session> {
  const response = await fetch(`/gw/kc/realms/${REALM}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'password',
      client_id: CLIENT_ID,
      username,
      password,
    }),
  })

  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw new Error(
      body.error_description === 'Invalid user credentials'
        ? 'E-posta veya parola hatalı.'
        : (body.error_description ?? 'Giriş yapılamadı.'),
    )
  }

  const token: TokenResponse = await response.json()
  return toSession(token.access_token)
}

/** JWT payload'ını çözer (imza doğrulaması backend'in işi, burada sadece okuma). */
export function toSession(accessToken: string): Session {
  const payload = decodeJwt(accessToken)

  return {
    token: accessToken,
    sub: payload.sub,
    email: payload.email ?? payload.preferred_username ?? '',
    fullName:
      [payload.given_name, payload.family_name].filter(Boolean).join(' ') ||
      (payload.preferred_username ?? ''),
    // Yetkilendirmede kullanılan roller Keycloak'ın realm_access claim'inden gelir
    roles: (payload.realm_access?.roles ?? []).filter(isAppRole),
    expiresAt: payload.exp * 1000,
  }
}

interface JwtPayload {
  sub: string
  exp: number
  email?: string
  preferred_username?: string
  given_name?: string
  family_name?: string
  realm_access?: { roles: string[] }
}

function decodeJwt(token: string): JwtPayload {
  const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
  // Türkçe karakterlerin bozulmaması için UTF-8 olarak çözülüyor
  const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0))
  return JSON.parse(new TextDecoder().decode(bytes))
}

const APP_ROLES: Role[] = ['admin', 'editor', 'user']
const isAppRole = (role: string): role is Role => (APP_ROLES as string[]).includes(role)
