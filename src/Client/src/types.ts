// Backend DTO'larının birebir karşılıkları.
// ASP.NET Core JSON'u camelCase ürettiği için alan adları burada camelCase.

export type Role = 'admin' | 'editor' | 'user'

/** Tüm servislerin döndüğü standart hata gövdesi (Shared.Models.ErrorResponse) */
export interface ErrorResponse {
  status: number
  errorCode: string
  message: string
  description: string
  errors: Record<string, string[]> | null
  timestamp: string
}

// ─── NewsService ──────────────────────────────────────────────────

export interface ArticleSummary {
  id: string
  title: string
  summary: string | null
  authorKeycloakId: string
  categoryName: string
  isPublished: boolean
  publishedAt: string | null
  createdAt: string
}

export interface ArticleDetail extends ArticleSummary {
  content: string
  imageUrl: string | null
  tags: string[]
}

export interface CreateArticleInput {
  title: string
  content: string
  summary?: string | null
  imageUrl?: string | null
  categoryId: string
  tagIds: string[]
}

export interface UpdateArticleInput {
  id: string
  title: string
  content: string
  summary?: string | null
  imageUrl?: string | null
  categoryId: string
}

export interface Category {
  id: string
  name: string
  description: string | null
  articleCount: number
}

export interface Tag {
  id: string
  name: string
  articleCount: number
}

// ─── IdentityService ──────────────────────────────────────────────

export interface AppUser {
  id: string
  email: string
  firstName: string
  lastName: string
  avatarUrl: string | null
  isActive: boolean
  roles: Role[]
}

export interface RegisterInput {
  email: string
  password: string
  firstName: string
  lastName: string
}

// ─── Keycloak ─────────────────────────────────────────────────────

export interface TokenResponse {
  access_token: string
  refresh_token: string
  expires_in: number
}

/** access_token'ın çözülmüş payload'ı */
export interface Session {
  token: string
  sub: string
  email: string
  fullName: string
  roles: Role[]
  expiresAt: number
}
