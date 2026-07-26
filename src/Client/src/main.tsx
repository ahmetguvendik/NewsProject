import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import { AuthProvider } from './auth/AuthContext'
import { Guard } from './components/Guard'
import { Layout } from './components/Layout'
import { ArticleDetailPage } from './pages/ArticleDetailPage'
import { ArticleEditorPage } from './pages/ArticleEditorPage'
import { ArticlesPage } from './pages/ArticlesPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { TaxonomyPage } from './pages/TaxonomyPage'
import { UsersPage } from './pages/UsersPage'

import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<Layout />}>
            {/* Herkese açık — backend'de [AllowAnonymous] */}
            <Route index element={<ArticlesPage />} />
            <Route path="haber/:id" element={<ArticleDetailPage />} />
            <Route path="giris" element={<LoginPage />} />
            <Route path="kayit" element={<RegisterPage />} />

            {/* editor + admin */}
            <Route
              path="haber/yeni"
              element={<Guard roles={['editor', 'admin']}><ArticleEditorPage /></Guard>}
            />
            <Route
              path="haber/:id/duzenle"
              element={<Guard roles={['editor', 'admin']}><ArticleEditorPage /></Guard>}
            />

            {/* yalnızca admin */}
            <Route path="taksonomi" element={<Guard roles={['admin']}><TaxonomyPage /></Guard>} />
            <Route path="kullanicilar" element={<Guard roles={['admin']}><UsersPage /></Guard>} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
