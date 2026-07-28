import { useEffect, useState } from 'react'
import { identityApi } from '../api/identity'

/**
 * Verilen Keycloak ID'lerini IdentityService'in dizin uç noktasından isme
 * çevirir. IdentityService erişilemezse veya ID silinmiş bir kullanıcıya
 * aitse harita boş kalır — çağıran taraf kısaltılmış ID'ye düşer.
 */
export function useAuthorNames(keycloakIds: string[]) {
  const [names, setNames] = useState<Map<string, string>>(new Map())

  // Bağımlılık dizisinde stabil bir anahtar kullanılıyor; aksi halde her
  // render'da yeni bir dizi referansı efekti tekrar tetikler.
  const key = [...new Set(keycloakIds)].sort().join(',')

  useEffect(() => {
    if (!key) {
      setNames(new Map())
      return
    }

    let cancelled = false

    identityApi
      .getDirectory(key.split(','))
      .then((entries) => {
        if (!cancelled) setNames(new Map(entries.map((e) => [e.keycloakId, e.displayName])))
      })
      .catch(() => {
        // Yazar adı ikincil bir bilgi — çözülemezse akış sessizce ID'ye düşer
        if (!cancelled) setNames(new Map())
      })

    return () => {
      cancelled = true
    }
  }, [key])

  return names
}

/** Harita boşsa veya ID kayıtlı değilse okunabilir bir yedek gösterir. */
export function displayAuthor(names: Map<string, string>, keycloakId: string) {
  return names.get(keycloakId) ?? `Kullanıcı ${keycloakId.slice(0, 8)}`
}
