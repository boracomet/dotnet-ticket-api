import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Layout } from '../components/Layout'

export function LoginPage() {
  const { login, token } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('user@ticket.local')
  const [password, setPassword] = useState('User1234!')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  if (token) return <Navigate to="/" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await login(email.trim(), password)
      navigate('/')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Giriş başarısız')
    } finally {
      setLoading(false)
    }
  }

  function fill(role: 'admin' | 'user') {
    if (role === 'admin') {
      setEmail('admin@ticket.local')
      setPassword('Admin123!')
    } else {
      setEmail('user@ticket.local')
      setPassword('User1234!')
    }
  }

  return (
    <Layout>
      <div className="auth-grid">
        <section className="auth-hero">
          <p className="eyebrow">Ticket API</p>
          <h1>Destek taleplerini yönetin</h1>
          <p>
            Katmanlı .NET 8 API üzerinde JWT, Admin/User rolleri, sayfalama,
            filtreleme ve sıkı durum geçiş kuralları.
          </p>
          <div className="seed-box">
            <strong>Demo hesaplar</strong>
            <button type="button" className="seed-btn" onClick={() => fill('user')}>
              user@ticket.local / User1234!
            </button>
            <button type="button" className="seed-btn" onClick={() => fill('admin')}>
              admin@ticket.local / Admin123!
            </button>
          </div>
        </section>
        <form className="card auth-card" onSubmit={onSubmit}>
          <h2>Giriş yap</h2>
          <p className="muted">Seed kullanıcılarla hemen deneyebilirsiniz.</p>
          {error && <div className="alert alert-error" role="alert">{error}</div>}
          <label>
            E-posta
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
          <label>
            Şifre
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
          <button className="btn btn-primary btn-block" disabled={loading} type="submit">
            {loading ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </button>
          <p className="auth-switch">
            Hesabınız yok mu? <Link to="/register">Kayıt olun</Link>
          </p>
        </form>
      </div>
    </Layout>
  )
}
