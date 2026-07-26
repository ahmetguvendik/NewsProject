import { api } from './http'
import type {
  ArticleDetail,
  ArticleSummary,
  Category,
  CreateArticleInput,
  Tag,
  UpdateArticleInput,
} from '../types'

export const newsApi = {
  // ─── Article ────────────────────────────────────────────────────
  listArticles: () => api.get<ArticleSummary[]>('news', '/api/article'),
  getArticle: (id: string) => api.get<ArticleDetail>('news', `/api/article/${id}`),
  createArticle: (input: CreateArticleInput) => api.post<{ id: string }>('news', '/api/article', input),
  updateArticle: (input: UpdateArticleInput) => api.put<{ id: string }>('news', '/api/article', input),
  deleteArticle: (id: string) => api.del<void>('news', `/api/article/${id}`),
  publishArticle: (id: string) => api.post<void>('news', `/api/article/${id}/publish`),

  // ─── Category ───────────────────────────────────────────────────
  listCategories: () => api.get<Category[]>('news', '/api/category'),
  createCategory: (input: { name: string; description?: string | null }) =>
    api.post<Category>('news', '/api/category', input),
  updateCategory: (input: { id: string; name: string; description?: string | null }) =>
    api.put<Category>('news', '/api/category', input),
  deleteCategory: (id: string) => api.del<void>('news', `/api/category/${id}`),

  // ─── Tag ────────────────────────────────────────────────────────
  listTags: () => api.get<Tag[]>('news', '/api/tag'),
  createTag: (input: { name: string }) => api.post<Tag>('news', '/api/tag', input),
  updateTag: (input: { id: string; name: string }) => api.put<Tag>('news', '/api/tag', input),
  deleteTag: (id: string) => api.del<void>('news', `/api/tag/${id}`),
}
