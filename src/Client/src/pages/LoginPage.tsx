import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)

    try {
      await signIn(email, password)
      const from = (location.state as { from?: string } | null)?.from
      navigate(from ?? '/', { replace: true })
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="card">
        <h2 className="card__title">Giriş yap</h2>
        <p className="card__sub">Kimlik doğrulama Keycloak üzerinden yapılır.</p>

        <ErrorAlert error={error} />

        <form onSubmit={submit}>
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
              autoComplete="current-password"
              required
            />
          </label>

          <button className="btn btn--primary btn--block" disabled={busy}>
            {busy ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </button>
        </form>

        <p className="field__hint" style={{ marginTop: 18 }}>
          Hesabınız yok mu? <Link to="/kayit" style={{ color: 'var(--accent)' }}>Kayıt olun</Link>
        </p>
      </div>
    </div>
  )
}
