import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { RoleBadges } from './RoleBadges'

export function Layout() {
  const { session, signOut, hasRole } = useAuth()
  const navigate = useNavigate()

  const navClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'is-active' : '')

  return (
    <>
      <header className="masthead">
        <div className="container">
          <div className="masthead__top">
            <div className="masthead__brand">
              <h1 className="masthead__title">News Portal</h1>
              <span className="masthead__tagline">Mikroservis Haber Sistemi</span>
            </div>

            <div className="masthead__user">
              {session ? (
                <>
                  <span className="masthead__name">{session.fullName || session.email}</span>
                  <RoleBadges roles={session.roles} />
                  <button
                    className="btn btn--sm"
                    onClick={() => {
                      signOut()
                      navigate('/')
                    }}
                  >
                    Çıkış
                  </button>
                </>
              ) : (
                <>
                  <NavLink to="/kayit" className="btn btn--sm">Kayıt ol</NavLink>
                  <NavLink to="/giris" className="btn btn--sm btn--primary">Giriş yap</NavLink>
                </>
              )}
            </div>
          </div>

          <nav className="nav">
            <NavLink to="/" end className={navClass}>Haberler</NavLink>
            {hasRole('editor', 'admin') && <NavLink to="/haber/yeni" className={navClass}>Yeni haber</NavLink>}
            {hasRole('admin') && <NavLink to="/taksonomi" className={navClass}>Kategori & Etiket</NavLink>}
            {hasRole('admin') && <NavLink to="/kullanicilar" className={navClass}>Kullanıcılar</NavLink>}
          </nav>
        </div>
      </header>

      <main className="container page">
        <Outlet />
      </main>
    </>
  )
}
