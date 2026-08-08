import { Navigate, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../auth/AuthContext'
import type { Role } from '../types'

/**
 * Route seviyesinde erişim kontrolü. Bu yalnızca arayüzü gizler —
 * asıl yetkilendirme backend'deki [Authorize] / [Authorize(Roles = ...)] ile yapılır.
 *
 * `roles` verilmezse yalnızca giriş yapmış olmak yeterlidir (ör. Hesabım sayfası).
 */
export function Guard({ roles, children }: { roles?: Role[]; children: ReactNode }) {
  const { session, hasRole } = useAuth()
  const location = useLocation()

  if (!session) return <Navigate to="/giris" state={{ from: location.pathname }} replace />

  if (roles && !hasRole(...roles)) {
    return (
      <div className="empty">
        <p className="empty__title">Bu sayfa için yetkiniz yok</p>
        <p>
          Gerekli rol: <strong>{roles.join(' veya ')}</strong>. Mevcut rolleriniz:{' '}
          <strong>{session.roles.join(', ') || 'yok'}</strong>.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
