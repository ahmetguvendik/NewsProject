import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { newsApi } from '../api/news'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/ErrorAlert'
import { formatDateTime, shortId } from '../lib/format'
import type { ArticleDetail } from '../types'

export function ArticleDetailPage() {
  const { id = '' } = useParams()
  const { hasRole } = useAuth()

  const [article, setArticle] = useState<ArticleDetail | null>(null)
  const [error, setError] = useState<unknown>(null)

  useEffect(() => {
    newsApi.getArticle(id).then(setArticle).catch(setError)
  }, [id])

  if (error) {
    return (
      <>
        <ErrorAlert error={error} />
        <Link className="btn" to="/">← Haberlere dön</Link>
      </>
    )
  }

  if (!article) return <div className="skeleton" style={{ height: 320 }} />

  return (
    <article className="reader">
      <div className="row row--between">
        <span className="kicker">{article.categoryName}</span>
        <span className={`badge badge--${article.isPublished ? 'published' : 'draft'}`}>
          {article.isPublished ? 'Yayında' : 'Taslak'}
        </span>
      </div>

      <h2 className="reader__title">{article.title}</h2>

      {article.summary && <p className="reader__summary">{article.summary}</p>}

      <div className="meta">
        <span>
          {article.isPublished && article.publishedAt
            ? formatDateTime(article.publishedAt)
            : `Taslak · ${formatDateTime(article.createdAt)}`}
        </span>
        <span>yazar: {shortId(article.authorKeycloakId)}…</span>
      </div>

      <hr className="reader__rule" />

      {article.imageUrl && <img className="reader__image" src={article.imageUrl} alt="" />}

      <div className="reader__body">{article.content}</div>

      {article.tags.length > 0 && (
        <div className="tags">
          {article.tags.map((tag) => (
            <span key={tag} className="tag-chip">#{tag}</span>
          ))}
        </div>
      )}

      <hr className="reader__rule" />

      <div className="row row--wrap">
        <Link className="btn" to="/">← Haberlere dön</Link>
        {hasRole('editor', 'admin') && (
          <Link className="btn" to={`/haber/${article.id}/duzenle`}>Düzenle</Link>
        )}
      </div>
    </article>
  )
}
