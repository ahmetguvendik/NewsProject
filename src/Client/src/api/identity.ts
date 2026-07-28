import { api } from './http'
import type { AppUser, RegisterInput, Role, UserDirectoryEntry } from '../types'

export const identityApi = {
  /** Anonim endpoint — Keycloak'ta kullanıcı açar, "user" rolü atar, event yayınlar */
  register: (input: RegisterInput) =>
    api.post<AppUser>('identity', '/api/auth/register', input),

  listUsers: () => api.get<AppUser[]>('identity', '/api/user'),
  getUser: (id: string) => api.get<AppUser>('identity', `/api/user/${id}`),
  deleteUser: (id: string) => api.del<void>('identity', `/api/user/${id}`),

  updateUser: (input: { id: string; firstName: string; lastName: string; avatarUrl?: string | null }) =>
    api.put<AppUser>('identity', '/api/user', input),

  /** Rolü hem Keycloak'a hem local DB'ye yazar */
  assignRole: (userId: string, roleName: Role) =>
    api.post<void>('identity', '/api/user/roles', { userId, roleName }),

  /**
   * NewsService yazar için yalnızca keycloakId tutuyor; görünen adı bu
   * [AllowAnonymous] uçtan çözüyoruz. Boş dizi çağrısı yapılmaz.
   */
  getDirectory: (keycloakIds: string[]) => {
    const unique = [...new Set(keycloakIds)]
    if (unique.length === 0) return Promise.resolve<UserDirectoryEntry[]>([])
    return api.get<UserDirectoryEntry[]>('identity', `/api/user/directory?ids=${unique.join(',')}`)
  },
}
