import { useCallback, useEffect, useState } from 'react'
import { identityApi } from '../api/identity'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'
import { Pagination } from '../components/Pagination'
import { RoleBadges } from '../components/RoleBadges'
import type { AppUser, Role } from '../types'

const ASSIGNABLE: Role[] = ['editor', 'admin']
const PAGE_SIZE = 20

const fullName = (user: AppUser) => `${user.firstName} ${user.lastName}`

export function UsersPage() {
  const { session } = useAuth()
  const [users, setUsers] = useState<AppUser[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setError(null)
    try {
      const result = await identityApi.listUsers(page, PAGE_SIZE)
      setUsers(result.items)
      setTotalCount(result.totalCount)
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [page])

  useEffect(() => {
    void load()
  }, [load])

  const assign = async (user: AppUser, role: Role) => {
    if (role === 'admin' && !confirm(`${fullName(user)} kullanıcısına admin yetkisi verilsin mi? Kullanıcıları ve tüm içeriği yönetebilir.`)) return
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

  const unassign = async (user: AppUser, role: Role) => {
    const selfAdmin = role === 'admin' && user.email === session?.email
      ? '\n\nKendi admin yetkinizi kaldırıyorsunuz; bu sayfaya bir daha erişemezsiniz.'
      : ''
    if (!confirm(`${fullName(user)} kullanıcısından "${role}" rolü kaldırılsın mı?${selfAdmin}`)) return
    setBusyId(user.id)
    setError(null)
    try {
      await identityApi.removeRole(user.id, role)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }


  const toggleActive = async (user: AppUser) => {
    // Delete'ten farklı: kullanıcı listede kalır, yalnızca login edemez hale gelir
    if (user.isActive && !confirm(`${fullName(user)} pasife alınsın mı? Oturumu hemen kapanır ve giriş yapamaz.`)) return
    setBusyId(user.id)
    setError(null)
    try {
      if (user.isActive) {
        await identityApi.deactivateUser(user.id)
      } else {
        await identityApi.activateUser(user.id)
      }
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
          <h1 className="page__title">Kullanıcılar</h1>
          <p className="page__sub">{totalCount} kullanıcı</p>
        </div>
      </div>

      <ErrorAlert error={error} />

      <div className="alert alert--info">
        Rol değişikliği hemen geçerli olur; kullanıcının yeniden giriş yapması gerekir.
      </div>

      {loading ? (
        <div className="skeleton" style={{ height: 240 }} />
      ) : (
        <>
          <div className="table-wrap">
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
                        {ASSIGNABLE.filter((role) => user.roles.includes(role)).map((role) => (
                          <button
                            key={role}
                            className="btn btn--sm btn--ghost"
                            disabled={busyId === user.id}
                            onClick={() => unassign(user, role)}
                            title={`${role} rolünü kaldır`}
                          >
                            − {role}
                          </button>
                        ))}
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
                          className="btn btn--sm"
                          disabled={busyId === user.id}
                          onClick={() => toggleActive(user)}
                          title={user.isActive ? 'Kullanıcı login edemez hale gelir' : 'Kullanıcı tekrar login edebilir'}
                        >
                          {user.isActive ? 'Pasife al' : 'Aktif et'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}
