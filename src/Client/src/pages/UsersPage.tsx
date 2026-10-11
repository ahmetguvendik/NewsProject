import { useCallback, useEffect, useState } from 'react'
import { identityApi } from '../api/identity'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'
import { Pagination } from '../components/Pagination'
import { coverStyle } from '../lib/cover'
import { initials } from '../lib/format'
import { ROLE_LABELS } from '../lib/roles'
import type { AppUser, Role } from '../types'

// Yetki tek kademe: admin editörün yapabildiği her şeyi zaten yapabiliyor.
const LEVELS: Role[] = ['user', 'editor', 'admin']
const PAGE_SIZE = 20

const fullName = (user: AppUser) => `${user.firstName} ${user.lastName}`

const levelOf = (user: AppUser): Role =>
  user.roles.includes('admin') ? 'admin' : user.roles.includes('editor') ? 'editor' : 'user'

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

  const run = async (user: AppUser, action: () => Promise<unknown>) => {
    setBusyId(user.id)
    setError(null)
    try {
      await action()
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  const changeRole = (user: AppUser, role: Role, isSelf: boolean) => {
    const current = levelOf(user)
    if (role === current) return

    if (role === 'admin') {
      if (!confirm(`${fullName(user)} kullanıcısına admin yetkisi verilsin mi? Kullanıcıları ve tüm içeriği yönetebilir.`)) return
    } else if (LEVELS.indexOf(role) < LEVELS.indexOf(current)) {
      const selfAdmin = current === 'admin' && isSelf
        ? '\n\nKendi admin yetkinizi kaldırıyorsunuz; bu sayfaya bir daha erişemezsiniz.'
        : ''
      if (!confirm(`${fullName(user)} kullanıcısının yetkisi ${ROLE_LABELS[current]} → ${ROLE_LABELS[role]} olarak düşürülsün mü?${selfAdmin}`)) return
    }

    void run(user, () => identityApi.setRole(user.id, role))
  }

  const toggleActive = (user: AppUser) => {
    if (user.isActive) {
      if (!confirm(`${fullName(user)} pasife alınsın mı? Oturumu hemen kapanır ve giriş yapamaz.`)) return
      void run(user, () => identityApi.deactivateUser(user.id))
    } else {
      void run(user, () => identityApi.activateUser(user.id))
    }
  }

  return (
    <>
      <div className="page__head">
        <div>
          <h1 className="page__title">Kullanıcılar</h1>
          <p className="page__sub">{loading ? 'Yükleniyor…' : `${totalCount} kullanıcı`}</p>
        </div>
      </div>

      <ErrorAlert error={error} />

      <div className="alert alert--info">
        Yetki değişikliği hemen geçerli olur; kullanıcının yeniden giriş yapması gerekir.
      </div>

      {loading ? (
        <div className="skeleton" style={{ height: 240 }} />
      ) : (
        <>
          <div className="table-wrap">
            <table className="table users-table">
              <thead>
                <tr>
                  <th>Kullanıcı</th>
                  <th>Yetki</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => {
                  const isSelf = user.email === session?.email
                  const busy = busyId === user.id
                  const level = levelOf(user)

                  return (
                    <tr key={user.id} className={user.isActive ? undefined : 'is-inactive'}>
                      <td>
                        <div className="user-cell">
                          <span className="avatar avatar--sm" style={coverStyle(null, user.email)} aria-hidden>
                            {initials(user)}
                          </span>
                          <div className="user-cell__text">
                            <div className="user-cell__name">
                              {fullName(user)}
                              {isSelf && <span className="badge badge--self">Siz</span>}
                            </div>
                            <div className="user-cell__mail">{user.email}</div>
                          </div>
                        </div>
                      </td>

                      <td>
                        <div className="role-seg" role="group" aria-label={`${fullName(user)} yetkisi`}>
                          {LEVELS.map((role) => (
                            <button
                              key={role}
                              type="button"
                              className={`role-seg__opt role-seg__opt--${role}`}
                              aria-pressed={level === role}
                              disabled={busy}
                              onClick={() => changeRole(user, role, isSelf)}
                            >
                              {ROLE_LABELS[role]}
                            </button>
                          ))}
                        </div>
                      </td>

                      <td>
                        <label
                          className="switch switch--live"
                          title={isSelf ? 'Kendi hesabınızı pasife alamazsınız' : undefined}
                        >
                          <input
                            type="checkbox"
                            role="switch"
                            checked={user.isActive}
                            disabled={busy || isSelf}
                            onChange={() => toggleActive(user)}
                          />
                          <span className="switch__track" />
                          <span className="switch__label">
                            {busy ? 'Kaydediliyor…' : user.isActive ? 'Aktif' : 'Pasif'}
                          </span>
                        </label>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <Pagination page={page} pageSize={PAGE_SIZE} totalCount={totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}
