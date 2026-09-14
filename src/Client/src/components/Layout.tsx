import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { SearchBox } from './SearchBox'
import { UserMenu } from './UserMenu'
import { WeatherWidget } from './WeatherWidget'

export function Layout() {
  const { session, hasRole } = useAuth()

  const navClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'is-active' : '')

  return (
    <>
      <header className="topbar">
        <div className="container topbar__row1">
          <div className="topbar__row1-side">
            <WeatherWidget />
          </div>

          <NavLink to="/" className="brand brand--centered">
            <span className="brand__mark" aria-hidden />
            <span className="brand__name">Telgraf</span>
          </NavLink>

          <div className="topbar__row1-side topbar__row1-side--right">
            <SearchBox />

            {session ? (
              <UserMenu />
            ) : (
              <div className="row" style={{ gap: 8 }}>
                <NavLink to="/kayit" className="btn btn--sm btn--ghost">Kayıt ol</NavLink>
                <NavLink to="/giris" className="btn btn--sm btn--primary">Giriş yap</NavLink>
              </div>
            )}
          </div>
        </div>

        <div className="container">
          <nav className="nav topbar__row2">
            <NavLink to="/" end className={navClass}>Akış</NavLink>
            {session && <NavLink to="/hesabim" className={navClass}>Hesabım</NavLink>}
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
