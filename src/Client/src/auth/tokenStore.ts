/**
 * Token'ın tek kaynağı. http katmanı da AuthContext de buradan okur.
 *
 * Neden context yerine modül? React'te çocuk efektleri ebeveyn efektlerinden
 * önce çalışıyor; token'ı AuthProvider'ın useEffect'i ile bağlarsak sayfa
 * yenilemesinde ilk API isteği başlıksız gidip 401 alıyor ve oturum sebepsiz
 * düşüyordu. Modül seviyesinde okuyunca sıralama sorunu ortadan kalkıyor.
 */

const STORAGE_KEY = 'telgraf.token'

/** "News Portal" adıyla açılmış oturumlar taşınır. Bir süre sonra silinebilir. */
const LEGACY_STORAGE_KEY = 'news-portal.token'

const CHANGE_EVENT = 'telgraf:token-changed'

export function readToken(): string | null {
  const legacy = localStorage.getItem(LEGACY_STORAGE_KEY)
  if (legacy) {
    if (!localStorage.getItem(STORAGE_KEY)) localStorage.setItem(STORAGE_KEY, legacy)
    localStorage.removeItem(LEGACY_STORAGE_KEY)
  }

  return localStorage.getItem(STORAGE_KEY)
}

export function writeToken(token: string) {
  localStorage.setItem(STORAGE_KEY, token)
  window.dispatchEvent(new Event(CHANGE_EVENT))
}

const LOGOUT_REASON_KEY = 'telgraf.logout-reason'

/** reason: oturum kullanıcı istemeden düştüyse giriş sayfasında gösterilecek açıklama. */
export function clearToken(reason?: string) {
  localStorage.removeItem(STORAGE_KEY)
  if (reason) sessionStorage.setItem(LOGOUT_REASON_KEY, reason)
  window.dispatchEvent(new Event(CHANGE_EVENT))
}

/** Bir kez okunur: sayfa yenilenince aynı açıklama tekrar çıkmaz. */
export function takeLogoutReason(): string | null {
  const reason = sessionStorage.getItem(LOGOUT_REASON_KEY)
  sessionStorage.removeItem(LOGOUT_REASON_KEY)
  return reason
}

/** Token dışarıdan (401 sonrası http katmanı ya da başka sekmede giriş/çıkış) değiştiğinde haber verir. */
export function onTokenChange(listener: () => void) {
  // storage olayı yalnızca diğer sekmelerde tetiklenir; key null ise localStorage.clear() çağrılmıştır.
  const onStorage = (event: StorageEvent) => {
    if (event.key === STORAGE_KEY || event.key === null) listener()
  }
  window.addEventListener(CHANGE_EVENT, listener)
  window.addEventListener('storage', onStorage)
  return () => {
    window.removeEventListener(CHANGE_EVENT, listener)
    window.removeEventListener('storage', onStorage)
  }
}
