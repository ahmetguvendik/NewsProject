import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { RoleBadges } from './RoleBadges'

/**
 * Üst bardaki kullanıcı menüsü: ada tıklayınca Profil ve Çıkış açılır.
 *
 * Önceden "Çıkış" doğrudan bir düğmeydi ve adın yanında duruyordu. İki sorunu
 * vardı: en sık kullanılmayacak eylem sürekli ekranda yer kaplıyordu ve profile
 * gitmenin tek yolu alt satırdaki gezinme bağlantısıydı — kullanıcılar hesap
 * işlerini alışkanlıkla adlarının altında arıyor.
 */
export function UserMenu() {
  const { session, signOut } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  // Dışarı tıklayınca ve Escape ile kapanır. Menü açıkken sayfanın geri kalanı
  // kullanılabilir durumda olduğu için ikisi de gerekiyor: kullanıcı menüyü
  // kapatmak için ille de aynı düğmeye dönmek zorunda kalmamalı.
  useEffect(() => {
    if (!open) return

    const onPointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false)
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  if (!session) return null

  const go = (path: string) => {
    setOpen(false)
    navigate(path)
  }

  return (
    <div className="usermenu" ref={containerRef}>
      <button
        type="button"
        className="usermenu__trigger"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        <span className="topbar__name">{session.fullName || session.email}</span>
        <RoleBadges roles={session.roles} />
        <svg className="usermenu__caret" viewBox="0 0 10 6" aria-hidden width="10" height="6">
          <path d="M1 1l4 4 4-4" fill="none" stroke="currentColor" strokeWidth="1.5"
                strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      {open && (
        <div className="usermenu__panel" role="menu">
          <button type="button" role="menuitem" className="usermenu__item"
                  onClick={() => go('/hesabim')}>
            Profil
          </button>

          <button
            type="button"
            role="menuitem"
            className="usermenu__item usermenu__item--danger"
            onClick={() => {
              setOpen(false)
              signOut()
              navigate('/')
            }}
          >
            Çıkış
          </button>
        </div>
      )}
    </div>
  )
}
