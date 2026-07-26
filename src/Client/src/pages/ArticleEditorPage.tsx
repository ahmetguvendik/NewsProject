import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { newsApi } from '../api/news'
import { ErrorAlert } from '../components/ErrorAlert'
import type { Category, Tag } from '../types'

export function ArticleEditorPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [categories, setCategories] = useState<Category[]>([])
  const [tags, setTags] = useState<Tag[]>([])

  const [title, setTitle] = useState('')
  const [summary, setSummary] = useState('')
  const [content, setContent] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [tagIds, setTagIds] = useState<string[]>([])

  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    Promise.all([newsApi.listCategories(), newsApi.listTags()])
      .then(([nextCategories, nextTags]) => {
        setCategories(nextCategories)
        setTags(nextTags)
        setCategoryId((current) => current || nextCategories[0]?.id || '')
      })
      .catch(setError)
  }, [])

  useEffect(() => {
    if (!id) return

    newsApi
      .getArticle(id)
      .then((article) => {
        setTitle(article.title)
        setSummary(article.summary ?? '')
        setContent(article.content)
        setImageUrl(article.imageUrl ?? '')
        // Detay yanıtı kategori adını döndürüyor, ID'yi değil — ada göre eşleştiriyoruz
        setCategories((current) => {
          const match = current.find((category) => category.name === article.categoryName)
          if (match) setCategoryId(match.id)
          return current
        })
      })
      .catch(setError)
  }, [id])

  const toggleTag = (tagId: string) =>
    setTagIds((current) =>
      current.includes(tagId) ? current.filter((value) => value !== tagId) : [...current, tagId],
    )

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
          imageUrl: imageUrl || null,
          categoryId,
        })
        navigate(`/haber/${id}`)
      } else {
        const created = await newsApi.createArticle({
          title,
          content,
          summary: summary || null,
          imageUrl: imageUrl || null,
          categoryId,
          tagIds,
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

        {!isEdit && (
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
            <p className="field__hint">
              Etiketler yalnızca oluşturma sırasında atanabiliyor — backend'in UpdateArticleCommand'ı
              etiket alanı içermiyor.
            </p>
          </div>
        )}

        <label className="field">
          <span className="field__label">Görsel URL</span>
          <input
            className="input"
            value={imageUrl}
            onChange={(event) => setImageUrl(event.target.value)}
            placeholder="https://…"
          />
        </label>

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
