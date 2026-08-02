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
  listArticles: () => api.get<ArticleSummary[]>('/api/article'),
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
