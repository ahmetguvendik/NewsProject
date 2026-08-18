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

/** Sayfalanmış liste yanıtları için ortak zarf (Shared.Models.PagedResult) */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

// ─── NewsService ──────────────────────────────────────────────────

export interface ArticleSummary {
  id: string
  title: string
  summary: string | null
  /** Kapak görseli — listede de dönüyor; yoksa kategoriden gradyan üretilir */
  imageUrl: string | null
  /**
   * Aynı görselin farklı genişlikteki kopyaları (`srcset` biçiminde). Dış
   * adreslerde ve varyant öncesi yüklenmiş kayıtlarda null gelir; o zaman
   * `imageUrl` tek başına kullanılır.
   */
  imageSrcset: string | null
  authorKeycloakId: string
  categoryName: string
  isPublished: boolean
  publishedAt: string | null
  createdAt: string
}

export interface ArticleDetail extends ArticleSummary {
  content: string
  /**
   * Veritabanındaki ham değer (depo anahtarı veya dış URL). Düzenleme formu
   * `imageUrl` yerine bunu geri gönderir; aksi halde her kayıtta anahtar
   * çözümlenmiş tam URL'e dönüşürdü.
   */
  imageKey: string | null
  categoryId: string
  /** Okuma ekranında gösterilen etiket adları */
  tags: string[]
  /** Düzenleme formunun mevcut etiketleri işaretlemesi için */
  tagIds: string[]
}

export interface CreateArticleInput {
  title: string
  content: string
  summary?: string | null
  imageUrl?: string | null
  categoryId: string
  tagIds: string[]
  /** Yayınlandığında bülten abonelerine bildirim gönderilsin mi? */
  notifySubscribers: boolean
}

export interface UpdateArticleInput {
  id: string
  title: string
  content: string
  summary?: string | null
  imageUrl?: string | null
  categoryId: string
  /** Nihai etiket kümesi — gönderilmeyenler kaldırılır */
  tagIds: string[]
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

// ─── Medya ────────────────────────────────────────────────────────

export interface MediaPolicy {
  maxSizeBytes: number
  allowedContentTypes: string[]
}

export interface PresignedUpload {
  uploadUrl: string
  key: string
  /** İmzaya dahil — PUT sırasında birebir bu değer gönderilmeli */
  contentType: string
  expiresInSeconds: number
}

export interface CommitUpload {
  /** Makaleye yazılacak kalıcı depo anahtarı */
  key: string
  /** Önizleme için çözümlenmiş adres */
  url: string | null
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

/** GET /api/user/directory — ID bulunamazsa dizide hiç yer almaz. */
export interface UserDirectoryEntry {
  keycloakId: string
  displayName: string
}

/** GET /api/user/me — giriş yapmış kullanıcının kendi profili */
export interface MyProfile {
  id: string
  keycloakId: string
  email: string
  firstName: string
  lastName: string
  isSubscribed: boolean
  isActive: boolean
  createdAt: string
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
