import { useEffect, useState } from 'react'
import { identityApi } from '../api/identity'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'
import { RoleBadges } from '../components/RoleBadges'
import { coverStyle } from '../lib/cover'
import { formatDate } from '../lib/format'
import type { MyProfile } from '../types'

export function AccountPage() {
  const { session } = useAuth()

  const [profile, setProfile] = useState<MyProfile | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    identityApi.getMyProfile().then(setProfile).catch(setError)
  }, [])

  const toggleSubscription = async () => {
    if (!profile) return

    const next = !profile.isSubscribed
    setBusy(true)
    setError(null)
    try {
      await identityApi.updateMySubscription(next)
      setProfile({ ...profile, isSubscribed: next })
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <div className="page__head">
        <div>
          <h2 className="page__title">Hesabım</h2>
          <p className="page__sub">Profil bilgileriniz ve bildirim tercihleriniz</p>
        </div>
      </div>

      <ErrorAlert error={error} />

      {!profile ? (
        <>
          <div className="skeleton" style={{ height: 112, marginBottom: 20 }} />
          <div className="account-grid">
            <div className="skeleton" style={{ height: 260 }} />
            <div className="skeleton" style={{ height: 200 }} />
          </div>
        </>
      ) : (
        <>
          <section className="account-hero">
            <span className="avatar" style={coverStyle(null, profile.email)} aria-hidden>
              {initials(profile)}
            </span>

            <div style={{ minWidth: 0 }}>
              <h3 className="account-hero__name">{profile.firstName} {profile.lastName}</h3>
              <p className="account-hero__mail">{profile.email}</p>
              <div className="row row--wrap" style={{ gap: 6 }}>
                <span className={`badge badge--${profile.isActive ? 'published' : 'draft'}`}>
                  {profile.isActive ? 'aktif' : 'pasif'}
                </span>
                <RoleBadges roles={session?.roles ?? []} />
              </div>
            </div>
          </section>

          <div className="account-grid">
            <section className="card">
              <h3 className="card__title">Hesap bilgileri</h3>
              <p className="card__sub">Bu bilgiler Keycloak ve IdentityService'ten geliyor.</p>

              <div className="info-row">
                <span className="info-row__label">Ad Soyad</span>
                <span className="info-row__value">{profile.firstName} {profile.lastName}</span>
              </div>
              <div className="info-row">
                <span className="info-row__label">E-posta</span>
                <span className="info-row__value">{profile.email}</span>
              </div>
              <div className="info-row">
                <span className="info-row__label">Üyelik tarihi</span>
                <span className="info-row__value">{formatDate(profile.createdAt)}</span>
              </div>
              <div className="info-row">
                <span className="info-row__label">Kullanıcı kimliği</span>
                <span className="info-row__value info-row__value--mono">{profile.keycloakId}</span>
              </div>
            </section>

            <section className="card">
              <h3 className="card__title">Bülten aboneliği</h3>
              <p className="card__sub">
                Editörlerin "abonelere bildir" olarak işaretlediği haberler yayınlandığında
                e-posta alırsınız. İstediğiniz zaman kapatabilirsiniz.
              </p>

              <label className="switch">
                <input
                  type="checkbox"
                  checked={profile.isSubscribed}
                  disabled={busy}
                  onChange={toggleSubscription}
                />
                <span className="switch__track" />
                <span className="switch__label">
                  {busy ? 'Kaydediliyor…' : profile.isSubscribed ? 'Abonelik açık' : 'Abonelik kapalı'}
                </span>
              </label>
            </section>
          </div>
        </>
      )}
    </>
  )
}

/** Görsel olmadığı için avatar baş harflerden oluşuyor. */
function initials(profile: MyProfile) {
  const letters = [profile.firstName, profile.lastName]
    .map((part) => part.trim()[0])
    .filter(Boolean)
    .join('')

  return letters || profile.email[0]
}
