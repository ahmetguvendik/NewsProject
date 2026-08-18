import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { newsApi } from '../api/news'
import { useAuth } from '../auth/AuthContext'
import { CoverImage } from '../components/CoverImage'
import { ErrorAlert } from '../components/ErrorAlert'
import { Pagination } from '../components/Pagination'
import { coverStyle } from '../lib/cover'
import { formatDate } from '../lib/format'
import type { ArticleSummary, Category } from '../types'

/** Yayınlanmışlarda yayın tarihi, taslaklarda oluşturulma tarihi esas alınır. */
const dateOf = (article: ArticleSummary) => article.publishedAt ?? article.createdAt

const PAGE_SIZE = 20

// Kapağın ekranda kapladığı genişlik — tarayıcı indireceği boyu buna bakarak
// seçiyor. Eşikler .feed__grid'in `minmax(300px, 1fr)` düzeninden geliyor:
// 3 sütun 984px'de, 2 sütun 666px'de sığmayı bırakıyor. Grid CSS'i değişirse
// bu değerler de güncellenmeli.
const CARD_COVER_SIZES = '(max-width: 666px) 100vw, (max-width: 984px) 50vw, 380px'

// Manşet kapağı 860px altında tam genişlik, üstünde 1.15fr/1fr bölünmeden payına
// düşen ~605 piksel.
const LEAD_COVER_SIZES = '(max-width: 860px) 100vw, 610px'

export function ArticlesPage() {
  const { hasRole } = useAuth()
  const navigate = useNavigate()

  // Filtre ve sayfa durumu URL'de tutuluyor — sayfa yenilense de kaybolmuyor,
  // adres paylaşılabiliyor. Filtrelemenin kendisi artık backend'de yapılıyor.
  const [searchParams, setSearchParams] = useSearchParams()
  const query = searchParams.get('q') ?? ''
  const activeCategory = searchParams.get('kategori') ?? ''
  const page = Number(searchParams.get('sayfa') ?? '1')

  const [articles, setArticles] = useState<ArticleSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  // Editör ve admin taslakları görür; okuyucular yalnızca yayındakileri.
  // (Asıl filtreleme backend'de, token'daki role bakılarak yapılıyor.)
  const canEdit = hasRole('editor', 'admin')
  const canPublish = hasRole('admin')

  const load = useCallback(async () => {
    setError(null)
    try {
      const [result, cats] = await Promise.all([
        newsApi.listArticles({
          category: activeCategory || undefined,
          search: query.trim() || undefined,
          page,
          pageSize: PAGE_SIZE,
        }),
        newsApi.listCategories(),
      ])
      setArticles(result.items)
      setTotalCount(result.totalCount)
      setCategories(cats)
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [activeCategory, query, page])

  useEffect(() => {
    void load()
  }, [load])

  /** Filtre değişince sayfa 1'e döner; yoksa 3. sayfadayken filtreleyip boş liste görülebilir. */
  const applyFilter = (next: { q?: string; kategori?: string; sayfa?: number }) => {
    const params: Record<string, string> = {}
    const nextQuery = next.q ?? query
    const nextCategory = next.kategori ?? activeCategory
    if (nextQuery.trim()) params.q = nextQuery
    if (nextCategory) params.kategori = nextCategory
    if (next.sayfa && next.sayfa > 1) params.sayfa = String(next.sayfa)
    setSearchParams(params)
  }

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
        style={coverStyle(article.imageUrl, article.categoryName)}
      >
        <CoverImage
          url={article.imageUrl}
          srcset={article.imageSrcset}
          sizes={lead ? LEAD_COVER_SIZES : CARD_COVER_SIZES}
          lazy={!lead}
        />
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

  // Öne çıkan haber yalnızca ilk sayfada, filtresiz görünümde anlamlı
  const isLeadLayout = page === 1
  const [lead, ...rest] = articles

  return (
    <>
      <div className="page__head">
        <div>
          <div className="feed-head">
            <h2 className="page__title">Akış</h2>

            {query.trim() ? (
              <button className="filter is-active" onClick={() => setSearchParams({})}>
                "{query}" için arama · temizle ✕
              </button>
            ) : (
              categories.length > 0 && (
                <nav className="filters" aria-label="Kategori filtresi">
                  <button
                    className={`filter${activeCategory === '' ? ' is-active' : ''}`}
                    onClick={() => applyFilter({ kategori: '', sayfa: 1 })}
                  >
                    Tümü
                  </button>
                  {categories.map((category) => (
                    <button
                      key={category.id}
                      className={`filter${activeCategory === category.name ? ' is-active' : ''}`}
                      onClick={() => applyFilter({ kategori: category.name, sayfa: 1 })}
                    >
                      {category.name}
                    </button>
                  ))}
                </nav>
              )
            )}
          </div>

          <p className="page__sub">
            {loading
              ? 'Yükleniyor…'
              : query.trim()
                ? `${totalCount} sonuç`
                : `${totalCount} haber${activeCategory ? ` · ${activeCategory}` : ''}`}
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
      ) : articles.length === 0 ? (
        <div className="empty">
          <p className="empty__title">
            {query.trim()
              ? `"${query}" için sonuç bulunamadı`
              : activeCategory ? `${activeCategory} kategorisinde haber yok` : 'Akış boş'}
          </p>
          <p>
            {query.trim() ? (
              <button className="btn btn--sm" onClick={() => setSearchParams({})}>Aramayı temizle</button>
            ) : activeCategory
              ? 'Başka bir kategori seçebilir veya tümüne dönebilirsiniz.'
              : canEdit
                ? 'İlk haberi yazmak için "Yeni haber" butonunu kullanın.'
                : 'Editörler haber yayınladığında burada görünecek.'}
          </p>
        </div>
      ) : (
        <>
          <div className="feed">
            {isLeadLayout ? (
              <>
                {renderStory(lead, true)}
                {rest.length > 0 && (
                  <div className="feed__grid">{rest.map((article) => renderStory(article))}</div>
                )}
              </>
            ) : (
              <div className="feed__grid">{articles.map((article) => renderStory(article))}</div>
            )}
          </div>

          <Pagination
            page={page}
            pageSize={PAGE_SIZE}
            totalCount={totalCount}
            onPageChange={(next) => applyFilter({ sayfa: next })}
          />
        </>
      )}
    </>
  )
}
