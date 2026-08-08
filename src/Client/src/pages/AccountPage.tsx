import { useEffect, useState } from 'react'
import { identityApi } from '../api/identity'
import { ErrorAlert } from '../components/ErrorAlert'
import type { MyProfile } from '../types'

export function AccountPage() {
  const [profile, setProfile] = useState<MyProfile | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    identityApi.getMyProfile().then(setProfile).catch(setError)
  }, [])

  const toggleSubscription = async () => {
    if (!profile) return

    setBusy(true)
    setError(null)
    try {
      const next = !profile.isSubscribed
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
          {profile && <p className="page__sub">{profile.email}</p>}
        </div>
      </div>

      <ErrorAlert error={error} />

      {!profile ? (
        <div className="skeleton" style={{ height: 180 }} />
      ) : (
        <div className="card" style={{ maxWidth: 640 }}>
          <h3 className="card__title">Bülten aboneliği</h3>
          <p className="card__sub">
            Abone olursanız, editörlerin "abonelere bildir" olarak işaretlediği haberler
            yayınlandığında e-posta alırsınız. İstediğiniz zaman kapatabilirsiniz.
          </p>

          <div className="row row--between" style={{ marginTop: 4 }}>
            <span className={`badge badge--${profile.isSubscribed ? 'published' : 'user'}`}>
              {profile.isSubscribed ? 'Abonelik açık' : 'Abonelik kapalı'}
            </span>

            <button
              className={`btn ${profile.isSubscribed ? '' : 'btn--primary'}`}
              disabled={busy}
              onClick={toggleSubscription}
            >
              {busy ? 'Kaydediliyor…' : profile.isSubscribed ? 'Abonelikten çık' : 'Bültene abone ol'}
            </button>
          </div>
        </div>
      )}
    </>
  )
}
