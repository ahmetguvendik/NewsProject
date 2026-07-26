import { useCallback, useEffect, useState } from 'react'
import { identityApi } from '../api/identity'
import { ErrorAlert } from '../components/ErrorAlert'
import { RoleBadges } from '../components/RoleBadges'
import type { AppUser, Role } from '../types'

const ASSIGNABLE: Role[] = ['editor', 'admin']

export function UsersPage() {
  const [users, setUsers] = useState<AppUser[]>([])
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setError(null)
    try {
      setUsers(await identityApi.listUsers())
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const assign = async (user: AppUser, role: Role) => {
    setBusyId(user.id)
    setError(null)
    try {
      await identityApi.assignRole(user.id, role)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  const remove = async (user: AppUser) => {
    if (!confirm(`${user.email} silinsin mi? DB'de soft-delete, Keycloak'ta devre dışı bırakılır.`)) return

    setBusyId(user.id)
    setError(null)
    try {
      await identityApi.deleteUser(user.id)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  return (
    <>
      <div className="page__head">
        <div>
          <h2 className="page__title">Kullanıcılar</h2>
          <p className="page__sub">{users.length} kullanıcı · IdentityService (:5001)</p>
        </div>
      </div>

      <ErrorAlert error={error} />

      <div className="alert alert--info">
        Rol atama hem Keycloak'a hem local veritabanına yazılır. <strong>Rol geri alma</strong>{' '}
        endpoint'i backend'de henüz yok — düşürmek için Keycloak konsolunu kullanın.
      </div>

      {loading ? (
        <div className="skeleton" style={{ height: 240 }} />
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Kullanıcı</th>
              <th>Roller</th>
              <th>Durum</th>
              <th className="right">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>
                  <strong>{user.firstName} {user.lastName}</strong>
                  <div style={{ color: 'var(--muted)', fontSize: 13 }}>{user.email}</div>
                </td>
                <td><RoleBadges roles={user.roles} /></td>
                <td>
                  <span className={`badge badge--${user.isActive ? 'published' : 'draft'}`}>
                    {user.isActive ? 'aktif' : 'pasif'}
                  </span>
                </td>
                <td className="right">
                  <div className="row row--wrap" style={{ justifyContent: 'flex-end' }}>
                    {ASSIGNABLE.filter((role) => !user.roles.includes(role)).map((role) => (
                      <button
                        key={role}
                        className="btn btn--sm"
                        disabled={busyId === user.id}
                        onClick={() => assign(user, role)}
                      >
                        + {role}
                      </button>
                    ))}
                    <button
                      className="btn btn--sm btn--danger"
                      disabled={busyId === user.id}
                      onClick={() => remove(user)}
                    >
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
