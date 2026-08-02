import { api } from './http'
import type { AppUser, PagedResult, RegisterInput, Role, UserDirectoryEntry } from '../types'

export const identityApi = {
  /** Anonim endpoint — Keycloak'ta kullanıcı açar, "user" rolü atar, event yayınlar */
  register: (input: RegisterInput) =>
    api.post<AppUser>('/api/auth/register', input),

  listUsers: (page = 1, pageSize = 20) =>
    api.get<PagedResult<AppUser>>(`/api/user?page=${page}&pageSize=${pageSize}`),
  getUser: (id: string) => api.get<AppUser>(`/api/user/${id}`),
  deleteUser: (id: string) => api.del<void>(`/api/user/${id}`),

  /** Delete'ten farklı: kullanıcı listede kalır, yalnızca login edemez hale gelir. Reversible. */
  deactivateUser: (id: string) => api.post<void>(`/api/user/${id}/deactivate`),
  activateUser: (id: string) => api.post<void>(`/api/user/${id}/activate`),

  updateUser: (input: { id: string; firstName: string; lastName: string; avatarUrl?: string | null }) =>
    api.put<AppUser>('/api/user', input),

  /** Rolü hem Keycloak'a hem local DB'ye yazar */
  assignRole: (userId: string, roleName: Role) =>
    api.post<void>('/api/user/roles', { userId, roleName }),

  /** Rolü hem Keycloak'tan hem local DB'den kaldırır */
  removeRole: (userId: string, roleName: Role) =>
    api.del<void>(`/api/user/${userId}/roles/${roleName}`),

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
