import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { RoleBadges } from './RoleBadges'

export function Layout() {
  const { session, signOut, hasRole } = useAuth()
  const navigate = useNavigate()

  const navClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'is-active' : '')

  return (
    <>
      <header className="topbar">
        <div className="container topbar__inner">
          <NavLink to="/" className="brand">
            <span className="brand__mark" aria-hidden />
            <span className="brand__name">Telgraf</span>
          </NavLink>

          <nav className="nav">
            <NavLink to="/" end className={navClass}>Akış</NavLink>
            {hasRole('editor', 'admin') && <NavLink to="/haber/yeni" className={navClass}>Yeni haber</NavLink>}
            {hasRole('admin') && <NavLink to="/taksonomi" className={navClass}>Kategori & Etiket</NavLink>}
            {hasRole('admin') && <NavLink to="/kullanicilar" className={navClass}>Kullanıcılar</NavLink>}
          </nav>

          <div className="topbar__user">
            {session ? (
              <>
                <span className="topbar__name">{session.fullName || session.email}</span>
                <RoleBadges roles={session.roles} />
                <button
                  className="btn btn--sm btn--ghost"
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
                <NavLink to="/kayit" className="btn btn--sm btn--ghost">Kayıt ol</NavLink>
                <NavLink to="/giris" className="btn btn--sm btn--primary">Giriş yap</NavLink>
              </>
            )}
          </div>
        </div>
      </header>

      <main className="container page">
        <Outlet />
      </main>
    </>
  )
}
