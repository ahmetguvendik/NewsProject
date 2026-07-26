import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { newsApi } from '../api/news'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'
import { formatDate, shortId } from '../lib/format'
import type { ArticleSummary } from '../types'

export function ArticlesPage() {
  const { hasRole } = useAuth()
  const navigate = useNavigate()

  const [articles, setArticles] = useState<ArticleSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      setArticles(await newsApi.listArticles())
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const publish = async (id: string) => {
    setBusyId(id)
    setError(null)
    try {
      await newsApi.publishArticle(id)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  const remove = async (id: string, title: string) => {
    if (!confirm(`"${title}" silinsin mi? Bu işlem makaleyi soft-delete yapar.`)) return

    setBusyId(id)
    setError(null)
    try {
      await newsApi.deleteArticle(id)
      await load()
    } catch (err) {
      setError(err)
    } finally {
      setBusyId(null)
    }
  }

  const canEdit = hasRole('editor', 'admin')
  const canPublish = hasRole('admin')

  return (
    <>
      <div className="page__head">
        <div>
          <h2 className="page__title">Haberler</h2>
          <p className="page__sub">
            {loading ? 'Yükleniyor…' : `${articles.length} haber · NewsService (:5002)`}
          </p>
        </div>
        {canEdit && (
          <button className="btn btn--primary" onClick={() => navigate('/haber/yeni')}>
            Yeni haber yaz
          </button>
        )}
      </div>

      <ErrorAlert error={error} />

      {loading ? (
        <div className="stack">
          <div className="skeleton" />
          <div className="skeleton" />
          <div className="skeleton" />
        </div>
      ) : articles.length === 0 ? (
        <div className="empty">
          <p className="empty__title">Henüz haber yok</p>
          <p>
            {canEdit
              ? 'İlk haberi yazmak için "Yeni haber yaz" butonunu kullanın.'
              : 'Editörler haber eklediğinde burada görünecek.'}
          </p>
        </div>
      ) : (
        <div className="articles">
          {articles.map((article) => (
            <article key={article.id} className="article-row">
              <div>
                <span className="kicker">{article.categoryName}</span>
                <h3 className="article-row__title">
                  <Link to={`/haber/${article.id}`}>{article.title}</Link>
                </h3>
                {article.summary && <p className="article-row__summary">{article.summary}</p>}
                <div className="meta">
                  <span className={`badge badge--${article.isPublished ? 'published' : 'draft'}`}>
                    {article.isPublished ? 'Yayında' : 'Taslak'}
                  </span>
                  <span>
                    {article.isPublished && article.publishedAt
                      ? `${formatDate(article.publishedAt)} tarihinde yayınlandı`
                      : `${formatDate(article.createdAt)} tarihinde oluşturuldu`}
                  </span>
                  <span>yazar: {shortId(article.authorKeycloakId)}…</span>
                </div>
              </div>

              <div className="article-row__actions">
                <Link className="btn btn--sm" to={`/haber/${article.id}`}>Oku</Link>
                {canEdit && (
                  <Link className="btn btn--sm" to={`/haber/${article.id}/duzenle`}>Düzenle</Link>
                )}
                {canPublish && !article.isPublished && (
                  <button
                    className="btn btn--sm btn--primary"
                    disabled={busyId === article.id}
                    onClick={() => publish(article.id)}
                  >
                    {busyId === article.id ? 'Yayınlanıyor…' : 'Yayınla'}
                  </button>
                )}
                {canEdit && (
                  <button
                    className="btn btn--sm btn--danger"
                    disabled={busyId === article.id}
                    onClick={() => remove(article.id, article.title)}
                  >
                    Sil
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
    </>
  )
}
