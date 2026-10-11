import { api } from './http'
import type { AppUser, MyProfile, PagedResult, RegisterInput, Role, UserDirectoryEntry } from '../types'

export const identityApi = {
  /** Giriş yapmış kullanıcının kendi profili — rol şartı yok */
  getMyProfile: () => api.get<MyProfile>('/api/user/me'),

  /** Bülten aboneliğini açar/kapatır */
  updateMySubscription: (isSubscribed: boolean) =>
    api.put<void>('/api/user/me/subscription', { isSubscribed }),

  /** Anonim endpoint — Keycloak'ta kullanıcı açar, "user" rolü atar, event yayınlar */
  register: (input: RegisterInput) =>
    api.post<AppUser>('/api/auth/register', input),

  listUsers: (page = 1, pageSize = 20) =>
    api.get<PagedResult<AppUser>>(`/api/user?page=${page}&pageSize=${pageSize}`),
  getUser: (id: string) => api.get<AppUser>(`/api/user/${id}`),

  /** Delete'ten farklı: kullanıcı listede kalır, yalnızca login edemez hale gelir. Reversible. */
  deactivateUser: (id: string) => api.post<void>(`/api/user/${id}/deactivate`),
  activateUser: (id: string) => api.post<void>(`/api/user/${id}/activate`),

  updateUser: (input: { id: string; firstName: string; lastName: string; avatarUrl?: string | null }) =>
    api.put<AppUser>('/api/user', input),

  /** Yetkiyi tek kademe olarak ayarlar (okuyucu/editör/admin); Keycloak ve local DB birlikte güncellenir */
  setRole: (userId: string, roleName: Role) =>
    api.put<void>(`/api/user/${userId}/role`, { roleName }),

  /**
   * NewsService yazar için yalnızca keycloakId tutuyor; görünen adı bu
   * [AllowAnonymous] uçtan çözüyoruz. Boş dizi çağrısı yapılmaz.
   */
  getDirectory: (keycloakIds: string[]) => {
    const unique = [...new Set(keycloakIds)]
    if (unique.length === 0) return Promise.resolve<UserDirectoryEntry[]>([])
    return api.get<UserDirectoryEntry[]>(`/api/user/directory?ids=${unique.join(',')}`)
  },
}
