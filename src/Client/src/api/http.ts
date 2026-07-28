import { clearToken, readToken } from '../auth/tokenStore'
import type { ErrorResponse } from '../types'

/**
 * Backend'in ErrorResponse gövdesini taşıyan hata tipi.
 * UI hem kısa `message`'ı hem de alan bazlı `errors` sözlüğünü gösterebilsin diye
 * ham gövde olduğu gibi korunuyor.
 */
export class ApiError extends Error {
  readonly status: number
  readonly errorCode: string
  readonly description: string
  readonly fieldErrors: Record<string, string[]> | null

  constructor(
    status: number,
    errorCode: string,
    message: string,
    description: string,
    fieldErrors: Record<string, string[]> | null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errorCode = errorCode
    this.description = description
    this.fieldErrors = fieldErrors
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    let body: Partial<ErrorResponse> = {}
    try {
      body = await response.json()
    } catch {
      // Gövde JSON değilse (ör. 502 HTML sayfası) varsayılanlara düşülür
    }

    return new ApiError(
      response.status,
      body.errorCode ?? 'UNKNOWN',
      body.message ?? `İstek başarısız oldu (HTTP ${response.status}).`,
      body.description ?? '',
      body.errors ?? null,
    )
  }
}

type Service = 'identity' | 'news'

async function request<T>(
  service: Service,
  path: string,
  init: RequestInit = {},
): Promise<T> {
  // Token doğrudan store'dan okunur — React efekt sırasına bağlı değil,
  // böylece sayfa yenilemesindeki ilk istek de başlıklı gider.
  const token = readToken()

  const response = await fetch(`/gw/${service}${path}`, {
    ...init,
    headers: {
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  })

  // Token süresi dolmuş veya geçersiz → oturumu düşür.
  // AuthContext bu değişikliği tokenStore olayı üzerinden dinliyor.
  if (response.status === 401) {
    clearToken()
    throw await ApiError.fromResponse(response)
  }

  if (!response.ok) throw await ApiError.fromResponse(response)

  // 204 No Content
  if (response.status === 204) return undefined as T

  return (await response.json()) as T
}

export const api = {
  get: <T>(service: Service, path: string) => request<T>(service, path),

  post: <T>(service: Service, path: string, body?: unknown) =>
    request<T>(service, path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),

  put: <T>(service: Service, path: string, body: unknown) =>
    request<T>(service, path, { method: 'PUT', body: JSON.stringify(body) }),

  del: <T>(service: Service, path: string) => request<T>(service, path, { method: 'DELETE' }),
}
