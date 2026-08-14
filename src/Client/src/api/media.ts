import { api } from './http'
import type { CommitUpload, MediaPolicy, PresignedUpload } from '../types'

/**
 * Görsel yükleme üç adımda ilerler ve dosya hiçbir zaman backend'den geçmez:
 * izin al → doğrudan depoya yükle → doğrulat.
 */
export const mediaApi = {
  getPolicy: () => api.get<MediaPolicy>('/api/media/policy'),

  /**
   * Dosyayı depoya yükleyip kalıcı anahtarını döndürür.
   * Dönen `key` makalenin görsel alanına yazılacak değerdir; `url` yalnızca önizleme içindir.
   */
  upload: async (file: File): Promise<CommitUpload> => {
    const ticket = await api.post<PresignedUpload>('/api/media/upload-url', {
      contentType: file.type,
      sizeBytes: file.size,
    })

    // İmzalı adrese doğrudan PUT. Content-Type imzaya dahil olduğu için
    // birebir aynı değer gönderilmeli, yoksa depo imzayı reddeder.
    const response = await fetch(ticket.uploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': ticket.contentType },
      body: file,
    })

    if (!response.ok) {
      throw new Error(`Dosya depoya yüklenemedi (HTTP ${response.status}).`)
    }

    return api.post<CommitUpload>('/api/media/commit', { key: ticket.key })
  },
}
