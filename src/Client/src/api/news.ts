import { api } from './http'
import type {
  ArticleDetail,
  ArticleSummary,
  Category,
  CreateArticleInput,
  PagedResult,
  Tag,
  UpdateArticleInput,
} from '../types'

export interface ListArticlesParams {
  category?: string
  search?: string
  page?: number
  pageSize?: number
}

export const newsApi = {
  // ─── Article ────────────────────────────────────────────────────
  listArticles: (params: ListArticlesParams = {}) => {
    const query = new URLSearchParams()
    if (params.category) query.set('category', params.category)
    if (params.search) query.set('search', params.search)
    query.set('page', String(params.page ?? 1))
    query.set('pageSize', String(params.pageSize ?? 20))
    return api.get<PagedResult<ArticleSummary>>(`/api/article?${query}`)
  },
  getArticle: (id: string) => api.get<ArticleDetail>(`/api/article/${id}`),
  createArticle: (input: CreateArticleInput) => api.post<{ id: string }>('/api/article', input),
  updateArticle: (input: UpdateArticleInput) => api.put<{ id: string }>('/api/article', input),
  deleteArticle: (id: string) => api.del<void>(`/api/article/${id}`),
  publishArticle: (id: string) => api.post<void>(`/api/article/${id}/publish`),

  // ─── Category ───────────────────────────────────────────────────
  listCategories: () => api.get<Category[]>('/api/category'),
  createCategory: (input: { name: string; description?: string | null }) =>
    api.post<Category>('/api/category', input),
  updateCategory: (input: { id: string; name: string; description?: string | null }) =>
    api.put<Category>('/api/category', input),
  deleteCategory: (id: string) => api.del<void>(`/api/category/${id}`),

  // ─── Tag ────────────────────────────────────────────────────────
  listTags: () => api.get<Tag[]>('/api/tag'),
  createTag: (input: { name: string }) => api.post<Tag>('/api/tag', input),
  updateTag: (input: { id: string; name: string }) => api.put<Tag>('/api/tag', input),
  deleteTag: (id: string) => api.del<void>(`/api/tag/${id}`),
}
