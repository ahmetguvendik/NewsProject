import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { newsApi } from '../api/news'
import { useAuth } from '../auth/AuthContext'
import { CoverImage } from '../components/CoverImage'
import { ErrorAlert } from '../components/ErrorAlert'
import { coverStyle } from '../lib/cover'
import { formatDateTime } from '../lib/format'
import { isUuid } from '../lib/id'
import { displayAuthor, useAuthorNames } from '../lib/useAuthorNames'
import type { ArticleDetail } from '../types'
import { NotFoundPage } from './NotFoundPage'

export function ArticleDetailPage() {
  const { id = '' } = useParams()
  const { hasRole, session } = useAuth()

  // Biçimi bozuk kimlik için istek hiç atılmıyor — gerekçesi lib/id.ts'te.
  const validId = isUuid(id)

  const canEdit = hasRole('editor', 'admin')

  const [article, setArticle] = useState<ArticleDetail | null>(null)
  const [error, setError] = useState<unknown>(null)

  useEffect(() => {
    if (!validId) return
    newsApi.getArticle(id).then(setArticle).catch(setError)
  }, [id, validId])

  const authorNames = useAuthorNames(article ? [article.authorKeycloakId] : [])

  // Erken dönüşler hook'ların ARDINDAN: sıra her render'da aynı kalmalı.
  if (!validId) return <NotFoundPage />

  if (error) {
    return (
      <>
        <ErrorAlert error={error} />
        <Link className="btn" to="/">← Akışa dön</Link>
      </>
    )
  }

  if (!article) return <div className="skeleton" style={{ height: 420 }} />

  return (
    <article className="reader">
      <div className="reader__cover" style={coverStyle(article.imageUrl, article.categoryName)}>
        {/* Okuma sütunu 740px'de sabitleniyor; altındaki genişlikte tam ekran. */}
        <CoverImage
          url={article.imageUrl}
          srcset={article.imageSrcset}
          sizes="(max-width: 788px) 100vw, 740px"
          lazy={false}
        />
      </div>

      <div className="row row--between">
        <span className="chip">{article.categoryName}</span>
        {/* Yayın durumu yalnızca içeriği yönetenleri ilgilendiriyor — akış
            sayfasında da aynı kural. Okuyucuya "Yayında" demek bilgi vermiyor:
            taslaklar zaten ona hiç ulaşmıyor (API 404 dönüyor), dolayısıyla
            gördüğü her haberde aynı rozet çıkıyordu. */}
        {canEdit && (
          <span className={`badge badge--${article.isPublished ? 'published' : 'draft'}`}>
            {article.isPublished ? 'Yayında' : 'Taslak'}
          </span>
        )}
      </div>

      <h2 className="reader__title">{article.title}</h2>

      {article.summary && <p className="reader__summary">{article.summary}</p>}

      <div className="meta">
        <span>
          {article.isPublished && article.publishedAt
            ? formatDateTime(article.publishedAt)
            : `Taslak · ${formatDateTime(article.createdAt)}`}
        </span>
        <span className="meta__dot">{displayAuthor(authorNames, article.authorKeycloakId)}</span>
      </div>

      <hr className="reader__rule" />

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
        <Link className="btn" to="/">← Akışa dön</Link>
        {/* Admin her haberi; editör yalnızca kendi yayınlanmamış taslağını.
            Aynı kural sunucuda da var — buradaki 403 dönecek bir düğmeyi
            göstermemek için. */}
        {(hasRole('admin') ||
          (!article.isPublished && article.authorKeycloakId === session?.sub)) && (
          <Link className="btn" to={`/haber/${article.id}/duzenle`}>Düzenle</Link>
        )}
      </div>
    </article>
  )
}
