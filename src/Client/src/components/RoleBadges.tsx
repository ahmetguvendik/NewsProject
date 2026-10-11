import { ROLE_LABELS } from '../lib/roles'
import type { Role } from '../types'

export function RoleBadges({ roles }: { roles: Role[] }) {
  // "user" rolü kayıt olan herkese otomatik atanır; ayrıca göstermek gürültü
  // yaratıyor — yalnızca editor/admin gibi ayırt edici roller rozet olarak çıkar.
  const notable = roles.filter((role) => role !== 'user')

  if (notable.length === 0) return null

  return (
    <span className="row" style={{ gap: 5 }}>
      {notable.map((role) => (
        <span key={role} className={`badge badge--${role}`}>
          {ROLE_LABELS[role]}
        </span>
      ))}
    </span>
  )
}
