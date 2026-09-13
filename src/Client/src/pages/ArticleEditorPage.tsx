import { useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { mediaApi } from '../api/media'
import { newsApi } from '../api/news'
import { ErrorAlert } from '../components/ErrorAlert'
import type { Category, MediaPolicy, Tag } from '../types'

export function ArticleEditorPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [categories, setCategories] = useState<Category[]>([])
  const [tags, setTags] = useState<Tag[]>([])

  const [title, setTitle] = useState('')
  const [summary, setSummary] = useState('')
  const [content, setContent] = useState('')
  // imageValue formun kaydedeceği ham değer: yüklenen dosyanın depo anahtarı
  // veya elle yapıştırılmış dış adres. imagePreview yalnızca gösterim içindir.
  const [imageValue, setImageValue] = useState('')
  const [imagePreview, setImagePreview] = useState<string | null>(null)
  const [categoryId, setCategoryId] = useState('')
  const [tagIds, setTagIds] = useState<string[]>([])
  const [notifySubscribers, setNotifySubscribers] = useState(false)

  // Etiket kataloğunun yüklenip yüklenmediği. Aşağıdaki "silinmiş etiket" tespiti
  // buna bakmak zorunda: katalog daha gelmemişken makalenin BÜTÜN etiketleri
  // katalogda yokmuş gibi görünür ve uyarı boş yere çıkardı.
  const [tagsLoaded, setTagsLoaded] = useState(false)

  const [policy, setPolicy] = useState<MediaPolicy | null>(null)
  const [uploading, setUploading] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    Promise.all([newsApi.listCategories(), newsApi.listTags()])
      .then(([nextCategories, nextTags]) => {
        setCategories(nextCategories)
        setTags(nextTags)
        setTagsLoaded(true)
        setCategoryId((current) => current || nextCategories[0]?.id || '')
      })
      .catch(setError)

    // Kurallar sunucudan gelir; limit iki yerde ayrı ayrı yazılmasın.
    mediaApi.getPolicy().then(setPolicy).catch(() => setPolicy(null))
  }, [])

  useEffect(() => {
    if (!id) return

    newsApi
      .getArticle(id)
      .then((article) => {
        setTitle(article.title)
        setSummary(article.summary ?? '')
        setContent(article.content)
        // Kaydedilecek değer ham anahtar, gösterilecek olan çözümlenmiş adres.
        setImageValue(article.imageKey ?? '')
        setImagePreview(article.imageUrl)
        setCategoryId(article.categoryId)
        setTagIds(article.tagIds)
      })
      .catch(setError)
  }, [id])

  const pickFile = async (file: File) => {
    setError(null)

    // Sunucu bu kuralları imzalı URL üretirken de uyguluyor; buradaki kontrol
    // kullanıcıyı boşuna yükleme yapmaktan kurtarmak için.
    if (policy && !policy.allowedContentTypes.includes(file.type)) {
      setError(new Error(`Bu dosya türü desteklenmiyor: ${file.type || 'bilinmiyor'}`))
      return
    }
    if (policy && file.size > policy.maxSizeBytes) {
      const mb = Math.round(policy.maxSizeBytes / (1024 * 1024))
      setError(new Error(`Dosya çok büyük. En fazla ${mb} MB yükleyebilirsiniz.`))
      return
    }

    setUploading(true)
    try {
      const result = await mediaApi.upload(file)
      setImageValue(result.key)
      setImagePreview(result.url)
    } catch (err) {
      setError(err)
    } finally {
      setUploading(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const clearImage = () => {
    setImageValue('')
    setImagePreview(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const toggleTag = (tagId: string) =>
    setTagIds((current) =>
      current.includes(tagId) ? current.filter((value) => value !== tagId) : [...current, tagId],
    )

  // Makalenin taşıdığı ama katalogda ARTIK OLMAYAN etiketler — yani makaleye
  // iliştirildikten sonra silinmiş olanlar.
  //
  // Neden ayrıca hesaplanıyor: kutucuklar katalogdan çiziliyor, dolayısıyla silinmiş
  // bir etiketin kutucuğu hiç çizilmiyor. Kullanıcı onu ekranda göremediği için
  // işaretini de kaldıramıyor; ama kimliği state'te durduğu ve her kaydetmede geri
  // gönderildiği için sunucu 404 TAG_NOT_FOUND dönüyordu. Sonuç: makale arayüzden
  // bir daha ASLA kaydedilemiyordu — çıkış yolu yoktu.
  //
  // Her render'da yeniden türetiliyor, state'e yazılmıyor: katalog ile makale iki
  // ayrı istekle geliyor ve hangisinin önce döneceği belli değil.
  const knownTagIds = new Set(tags.map((tag) => tag.id))
  const missingTagIds = tagsLoaded ? tagIds.filter((tagId) => !knownTagIds.has(tagId)) : []

  // Sunucuya yalnızca kullanıcının ekranda görüp değiştirebildiği etiketler gidiyor.
  const submittableTagIds = tagsLoaded ? tagIds.filter((tagId) => knownTagIds.has(tagId)) : tagIds

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setError(null)

    try {
      if (isEdit && id) {
        await newsApi.updateArticle({
          id,
          title,
          content,
          summary: summary || null,
          imageUrl: imageValue || null,
          categoryId,
          tagIds: submittableTagIds,
        })
        navigate(`/haber/${id}`)
      } else {
        const created = await newsApi.createArticle({
          title,
          content,
          summary: summary || null,
          imageUrl: imageValue || null,
          categoryId,
          tagIds: submittableTagIds,
          notifySubscribers,
        })
        navigate(`/haber/${created.id}`)
      }
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <div className="page__head">
        <div>
          <h2 className="page__title">{isEdit ? 'Haberi düzenle' : 'Yeni haber'}</h2>
          <p className="page__sub">
            {isEdit
              ? 'Kaydedince makale güncellenir — yayın durumu değişmez.'
              : 'Kaydedince taslak oluşur; yayınlamayı admin yapar.'}
          </p>
        </div>
      </div>

      <ErrorAlert error={error} />

      {categories.length === 0 && (
        <div className="alert alert--info">
          Önce en az bir <strong>kategori</strong> tanımlanmalı. Makale oluşturmak zorunlu olarak bir
          kategori istiyor.
        </div>
      )}

      <form className="card" onSubmit={submit}>
        <label className="field">
          <span className="field__label">Başlık</span>
          <input
            className="input"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            placeholder="Manşet"
            required
          />
        </label>

        <label className="field">
          <span className="field__label">Özet</span>
          <input
            className="input"
            value={summary}
            onChange={(event) => setSummary(event.target.value)}
            placeholder="Listede görünecek kısa açıklama"
          />
        </label>

        <label className="field">
          <span className="field__label">Kategori</span>
          <select
            className="select"
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            required
          >
            {categories.map((category) => (
              <option key={category.id} value={category.id}>{category.name}</option>
            ))}
          </select>
        </label>

        <div className="field">
          <span className="field__label">Etiketler</span>
          {tags.length === 0 ? (
            <p className="field__hint">Henüz etiket tanımlanmamış.</p>
          ) : (
            <div className="checks">
              {tags.map((tag) => (
                <label key={tag.id} className="check">
                  <input
                    type="checkbox"
                    checked={tagIds.includes(tag.id)}
                    onChange={() => toggleTag(tag.id)}
                  />
                  {tag.name}
                </label>
              ))}
            </div>
          )}
          {missingTagIds.length > 0 && (
            // Sessizce düşürmek yerine söylüyoruz: kullanıcı kaydettiğinde makalenin
            // etiketlerinin değişeceğini bilsin. Etiket zaten silinmiş durumda, yani
            // geri getirilebilecek bir şey yok — yapılabilecek tek şey haber vermek.
            <p className="field__hint">
              Bu makaledeki {missingTagIds.length} etiket silinmiş. Kaydettiğinizde
              makaleden de çıkarılacak.
            </p>
          )}
          {isEdit && (
            <p className="field__hint">
              İşareti kaldırılan etiketler makaleden çıkarılır.
            </p>
          )}
        </div>

        {!isEdit && (
          <div className="field">
            <span className="field__label">Bildirim</span>
            <label className="check">
              <input
                type="checkbox"
                checked={notifySubscribers}
                onChange={(event) => setNotifySubscribers(event.target.checked)}
              />
              Abonelere bildir
            </label>
            <p className="field__hint">
              İşaretlenirse, bu haber <strong>yayınlandığında</strong> bülten abonelerine e-posta
              gönderilir. Taslak kaydetmek tek başına bildirim göndermez.
            </p>
          </div>
        )}

        <div className="field">
          <span className="field__label">Kapak görseli</span>

          {imagePreview && (
            <div className="cover-preview">
              <img src={imagePreview} alt="Kapak önizlemesi" />
            </div>
          )}

          <div className="row">
            <button
              type="button"
              className="btn btn--sm"
              disabled={uploading}
              onClick={() => fileInputRef.current?.click()}
            >
              {uploading ? 'Yükleniyor…' : imagePreview ? 'Görseli değiştir' : 'Görsel yükle'}
            </button>

            {imageValue && (
              <button type="button" className="btn btn--sm btn--ghost" onClick={clearImage}>
                Kaldır
              </button>
            )}
          </div>

          <input
            ref={fileInputRef}
            type="file"
            hidden
            accept={policy?.allowedContentTypes.join(',')}
            onChange={(event) => {
              const file = event.target.files?.[0]
              if (file) void pickFile(file)
            }}
          />

          <input
            className="input"
            value={imageValue}
            onChange={(event) => {
              setImageValue(event.target.value)
              setImagePreview(event.target.value || null)
            }}
            placeholder="veya bir adres yapıştırın: https://…"
          />

          <p className="field__hint">
            {policy
              ? `${policy.allowedContentTypes
                  .map((type) => type.replace('image/', '').toUpperCase())
                  .join(', ')} · en fazla ${Math.round(policy.maxSizeBytes / (1024 * 1024))} MB`
              : 'Dosya doğrudan depoya yüklenir.'}
          </p>
        </div>

        <label className="field">
          <span className="field__label">İçerik</span>
          <textarea
            className="textarea"
            value={content}
            onChange={(event) => setContent(event.target.value)}
            placeholder="Haber metni…"
            required
          />
        </label>

        <div className="row">
          <button className="btn btn--primary" disabled={saving || !categoryId}>
            {saving ? 'Kaydediliyor…' : isEdit ? 'Değişiklikleri kaydet' : 'Taslağı kaydet'}
          </button>
          <button type="button" className="btn btn--ghost" onClick={() => navigate(-1)}>
            Vazgeç
          </button>
        </div>
      </form>
    </>
  )
}
