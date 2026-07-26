import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { identityApi } from '../api/identity'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'

export function RegisterPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)

    try {
      await identityApi.register({ email, password, firstName, lastName })
      // Kayıt Keycloak'ta kullanıcıyı da açtığı için hemen giriş yapılabiliyor
      await signIn(email, password)
      navigate('/', { replace: true })
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="card">
        <h2 className="card__title">Kayıt ol</h2>
        <p className="card__sub">
          Kayıt olan herkese otomatik <strong>okuyucu</strong> rolü atanır.
        </p>

        <ErrorAlert error={error} />

        <form onSubmit={submit}>
          <div className="panel-grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            <label className="field">
              <span className="field__label">Ad</span>
              <input
                className="input"
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                required
              />
            </label>

            <label className="field">
              <span className="field__label">Soyad</span>
              <input
                className="input"
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                required
              />
            </label>
          </div>

          <label className="field">
            <span className="field__label">E-posta</span>
            <input
              className="input"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="username"
              required
            />
          </label>

          <label className="field">
            <span className="field__label">Parola</span>
            <input
              className="input"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="new-password"
              required
            />
          </label>

          <button className="btn btn--primary btn--block" disabled={busy}>
            {busy ? 'Kaydediliyor…' : 'Kayıt ol'}
          </button>
        </form>

        <p className="field__hint" style={{ marginTop: 18 }}>
          Kayıt sonrası hoşgeldin e-postası outbox → Kafka → inbox zinciriyle asenkron gönderilir.
        </p>

        <p className="field__hint">
          Zaten hesabınız var mı? <Link to="/giris" style={{ color: 'var(--accent)' }}>Giriş yapın</Link>
        </p>
      </div>
    </div>
  )
}
