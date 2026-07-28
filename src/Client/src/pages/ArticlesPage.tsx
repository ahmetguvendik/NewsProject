import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { newsApi } from '../api/news'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'
import { coverStyle } from '../lib/cover'
import { formatDate } from '../lib/format'
import type { ArticleSummary, Category } from '../types'

/** Yayınlanmışlarda yayın tarihi, taslaklarda oluşturulma tarihi esas alınır. */
const dateOf = (article: ArticleSummary) => article.publishedAt ?? article.createdAt

const ALL = '__all__'

export function ArticlesPage() {
  const { hasRole } = useAuth()
  const navigate = useNavigate()

  const [articles, setArticles] = useState<ArticleSummary[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [activeCategory, setActiveCategory] = useState<string>(ALL)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  // Editör ve admin taslakları görür; okuyucular yalnızca yayındakileri.
  const canEdit = hasRole('editor', 'admin')
  const canPublish = hasRole('admin')

  const load = useCallback(async () => {
    setError(null)
    try {
      const [list, cats] = await Promise.all([newsApi.listArticles(), newsApi.listCategories()])
      // Backend GetAllArticles'ta OrderBy yok — en yeni haber öne çıksın diye
      // sıralamayı burada yapıyoruz.
      list.sort((a, b) => +new Date(dateOf(b)) - +new Date(dateOf(a)))
      setArticles(list)
      setCategories(cats)
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const visible = useMemo(
    () =>
      articles
        // Okuyucuya taslak gösterilmez; backend hepsini döndürdüğü için burada eleniyor
        .filter((article) => canEdit || article.isPublished)
        .filter((article) => activeCategory === ALL || article.categoryName === activeCategory),
    [articles, activeCategory, canEdit],
  )

  // Hiç yayınlanmış haberi olmayan kategoriler okuyucuya filtre olarak gösterilmez
  const visibleCategories = useMemo(() => {
    const withContent = new Set(
      articles.filter((article) => canEdit || article.isPublished).map((a) => a.categoryName),
    )
    return categories.filter((category) => withContent.has(category.name))
  }, [categories, articles, canEdit])

  const act = async (id: string, action: () => Promise<unknown>) => {
    setBusyId(id)
    setError(null)
    try {
      await action()
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  const renderStory = (article: ArticleSummary, lead = false) => (
    <article key={article.id} className={`story${lead ? ' story--lead' : ''}`}>
      <Link
        to={`/haber/${article.id}`}
        className="story__cover"
        style={coverStyle(null, article.categoryName)}
      >
        <span className="chip">{article.categoryName}</span>
      </Link>

      <div className="story__body">
        <h3 className="story__title">
          <Link to={`/haber/${article.id}`}>{article.title}</Link>
        </h3>

        {article.summary && <p className="story__summary">{article.summary}</p>}

        <div className="story__foot">
          {/* Yayın durumu yalnızca içeriği yönetenleri ilgilendiriyor */}
          {canEdit && (
            <span className={`badge badge--${article.isPublished ? 'published' : 'draft'}`}>
              {article.isPublished ? 'Yayında' : 'Taslak'}
            </span>
          )}
          <span className="meta">{formatDate(dateOf(article))}</span>
        </div>

        {canEdit && (
          <div className="story__actions">
            {canPublish && !article.isPublished && (
              <button
                className="btn btn--sm btn--primary"
                disabled={busyId === article.id}
                onClick={() => act(article.id, () => newsApi.publishArticle(article.id))}
              >
                {busyId === article.id ? 'Yayınlanıyor…' : 'Yayınla'}
              </button>
            )}
            <Link className="btn btn--sm" to={`/haber/${article.id}/duzenle`}>Düzenle</Link>
            <button
              className="btn btn--sm btn--danger"
              disabled={busyId === article.id}
              onClick={() => {
                if (confirm(`"${article.title}" silinsin mi?`)) {
                  void act(article.id, () => newsApi.deleteArticle(article.id))
                }
              }}
            >
              Sil
            </button>
          </div>
        )}
      </div>
    </article>
  )

  const [lead, ...rest] = visible

  return (
    <>
      <div className="page__head">
        <div>
          <div className="feed-head">
            <h2 className="page__title">Akış</h2>

            {visibleCategories.length > 0 && (
              <nav className="filters" aria-label="Kategori filtresi">
                <button
                  className={`filter${activeCategory === ALL ? ' is-active' : ''}`}
                  onClick={() => setActiveCategory(ALL)}
                >
                  Tümü
                </button>
                {visibleCategories.map((category) => (
                  <button
                    key={category.id}
                    className={`filter${activeCategory === category.name ? ' is-active' : ''}`}
                    onClick={() => setActiveCategory(category.name)}
                  >
                    {category.name}
                  </button>
                ))}
              </nav>
            )}
          </div>

          <p className="page__sub">
            {loading
              ? 'Yükleniyor…'
              : `${visible.length} haber${activeCategory === ALL ? '' : ` · ${activeCategory}`}`}
          </p>
        </div>

        {canEdit && (
          <button className="btn btn--primary" onClick={() => navigate('/haber/yeni')}>
            Yeni haber
          </button>
        )}
      </div>

      <ErrorAlert error={error} />

      {loading ? (
        <div className="feed">
          <div className="skeleton" style={{ height: 280 }} />
          <div className="feed__grid">
            <div className="skeleton" style={{ height: 300 }} />
            <div className="skeleton" style={{ height: 300 }} />
            <div className="skeleton" style={{ height: 300 }} />
          </div>
        </div>
      ) : visible.length === 0 ? (
        <div className="empty">
          <p className="empty__title">
            {activeCategory === ALL ? 'Akış boş' : `${activeCategory} kategorisinde haber yok`}
          </p>
          <p>
            {activeCategory !== ALL
              ? 'Başka bir kategori seçebilir veya tümüne dönebilirsiniz.'
              : canEdit
                ? 'İlk haberi yazmak için "Yeni haber" butonunu kullanın.'
                : 'Editörler haber yayınladığında burada görünecek.'}
          </p>
        </div>
      ) : (
        <div className="feed">
          {renderStory(lead, true)}
          {rest.length > 0 && <div className="feed__grid">{rest.map((article) => renderStory(article))}</div>}
        </div>
      )}
    </>
  )
}
