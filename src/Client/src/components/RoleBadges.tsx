import type { Role } from '../types'

const LABELS: Record<Role, string> = {
  admin: 'admin',
  editor: 'editör',
  user: 'okuyucu',
}

export function RoleBadges({ roles }: { roles: Role[] }) {
  if (roles.length === 0) return <span className="badge badge--user">rolsüz</span>

  return (
    <span className="row" style={{ gap: 5 }}>
      {roles.map((role) => (
        <span key={role} className={`badge badge--${role}`}>
          {LABELS[role]}
        </span>
      ))}
    </span>
  )
}
