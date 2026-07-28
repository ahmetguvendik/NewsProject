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

export function clearToken() {
  localStorage.removeItem(STORAGE_KEY)
  window.dispatchEvent(new Event(CHANGE_EVENT))
}

/** Token dışarıdan (ör. 401 sonrası http katmanından) değiştiğinde haber verir. */
export function onTokenChange(listener: () => void) {
  window.addEventListener(CHANGE_EVENT, listener)
  return () => window.removeEventListener(CHANGE_EVENT, listener)
}
