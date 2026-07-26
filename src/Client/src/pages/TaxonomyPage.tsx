import { useCallback, useEffect, useState } from 'react'
import { newsApi } from '../api/news'
import { ErrorAlert } from '../components/ErrorAlert'
import type { Category, Tag } from '../types'

export function TaxonomyPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [tags, setTags] = useState<Tag[]>([])
  const [error, setError] = useState<unknown>(null)

  const [categoryName, setCategoryName] = useState('')
  const [categoryDescription, setCategoryDescription] = useState('')
  const [tagName, setTagName] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      const [nextCategories, nextTags] = await Promise.all([
        newsApi.listCategories(),
        newsApi.listTags(),
      ])
      setCategories(nextCategories)
      setTags(nextTags)
    } catch (err) {
      setError(err)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true)
    setError(null)
    try {
      await action()
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const addCategory = (event: React.FormEvent) => {
    event.preventDefault()
    void run(async () => {
      await newsApi.createCategory({ name: categoryName, description: categoryDescription || null })
      setCategoryName('')
      setCategoryDescription('')
    })
  }

  const addTag = (event: React.FormEvent) => {
    event.preventDefault()
    void run(async () => {
      await newsApi.createTag({ name: tagName })
      setTagName('')
    })
  }

  return (
    <>
      <div className="page__head">
        <div>
          <h2 className="page__title">Kategori & Etiket</h2>
          <p className="page__sub">
            Her ikisi de benzersiz ada sahip — aynı isim ikinci kez eklenirse backend 409 döner.
          </p>
        </div>
      </div>

      <ErrorAlert error={error} />

      <div className="panel-grid">
        <section className="card">
          <h3 className="card__title">Kategoriler</h3>
          <p className="card__sub">Her haber zorunlu olarak bir kategoriye bağlı.</p>

          <form onSubmit={addCategory}>
            <label className="field">
              <span className="field__label">Ad</span>
              <input
                className="input"
                value={categoryName}
                onChange={(event) => setCategoryName(event.target.value)}
                placeholder="Ekonomi"
                required
              />
            </label>

            <label className="field">
              <span className="field__label">Açıklama</span>
              <input
                className="input"
                value={categoryDescription}
                onChange={(event) => setCategoryDescription(event.target.value)}
                placeholder="İsteğe bağlı"
              />
            </label>

            <button className="btn btn--primary" disabled={busy}>Kategori ekle</button>
          </form>

          <table className="table" style={{ marginTop: 22 }}>
            <thead>
              <tr>
                <th>Ad</th>
                <th className="right">Haber</th>
                <th className="right"></th>
              </tr>
            </thead>
            <tbody>
              {categories.length === 0 && (
                <tr><td colSpan={3} style={{ color: 'var(--muted)' }}>Kategori yok.</td></tr>
              )}
              {categories.map((category) => (
                <tr key={category.id}>
                  <td>
                    <strong>{category.name}</strong>
                    {category.description && (
                      <div style={{ color: 'var(--muted)', fontSize: 13 }}>{category.description}</div>
                    )}
                  </td>
                  <td className="right">{category.articleCount}</td>
                  <td className="right">
                    <button
                      className="btn btn--sm btn--danger"
                      disabled={busy}
                      onClick={() => void run(() => newsApi.deleteCategory(category.id))}
                    >
                      Sil
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section className="card">
          <h3 className="card__title">Etiketler</h3>
          <p className="card__sub">Haberlere yalnızca oluşturma sırasında iliştirilebilir.</p>

          <form onSubmit={addTag}>
            <label className="field">
              <span className="field__label">Ad</span>
              <input
                className="input"
                value={tagName}
                onChange={(event) => setTagName(event.target.value)}
                placeholder="enflasyon"
                required
              />
            </label>

            <button className="btn btn--primary" disabled={busy}>Etiket ekle</button>
          </form>

          <table className="table" style={{ marginTop: 22 }}>
            <thead>
              <tr>
                <th>Ad</th>
                <th className="right">Haber</th>
                <th className="right"></th>
              </tr>
            </thead>
            <tbody>
              {tags.length === 0 && (
                <tr><td colSpan={3} style={{ color: 'var(--muted)' }}>Etiket yok.</td></tr>
              )}
              {tags.map((tag) => (
                <tr key={tag.id}>
                  <td><strong>#{tag.name}</strong></td>
                  <td className="right">{tag.articleCount}</td>
                  <td className="right">
                    <button
                      className="btn btn--sm btn--danger"
                      disabled={busy}
                      onClick={() => void run(() => newsApi.deleteTag(tag.id))}
                    >
                      Sil
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>
    </>
  )
}
