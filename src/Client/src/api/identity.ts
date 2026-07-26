import { api } from './http'
import type { AppUser, RegisterInput, Role } from '../types'

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
}
