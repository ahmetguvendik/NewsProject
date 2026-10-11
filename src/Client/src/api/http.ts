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

async function request<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  // Token doğrudan store'dan okunur — React efekt sırasına bağlı değil,
  // böylece sayfa yenilemesindeki ilk istek de başlıklı gider.
  const token = readToken()

  // /gw → Vite dev proxy → APISIX (:9080). path zaten "/api/..." ile başlıyor
  // (gerçek backend route'u), bu yüzden ayrıca "api" önekine gerek yok.
  // Path'ler servisler arasında çakışmadığı için hangi backend'e gideceğine
  // APISIX'in kendisi karar veriyor — client bilmek zorunda değil.
  const response = await fetch(`/gw${path}`, {
    ...init,
    headers: {
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  })

  // Token geçersiz ya da hesap pasife alınmış → oturumu düşür.
  // AuthContext bu değişikliği tokenStore olayı üzerinden dinliyor.
  if (!response.ok) {
    const error = await ApiError.fromResponse(response)
    // Yalnızca isteği atan token hâlâ geçerliyse: aynı anda dönen ikinci 401 ya da
    // yeni girişten önceki eski bir istek oturumu (ve açıklamayı) tekrar düşürmesin.
    const sessionEnded = response.status === 401 || error.errorCode === 'AUTH_ACCOUNT_DISABLED'
    if (sessionEnded && token && readToken() === token) clearToken(`${error.message} ${error.description}`)
    throw error
  }

  // 204 No Content
  if (response.status === 204) return undefined as T

  return (await response.json()) as T
}

export const api = {
  get: <T>(path: string) => request<T>(path),

  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),

  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),

  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
