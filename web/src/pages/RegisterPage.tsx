import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Layout } from '../components/Layout'

export function RegisterPage() {
  const { register, token } = useAuth()
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  if (token) return <Navigate to="/" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await register(email.trim(), fullName.trim(), password)
      navigate('/')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kayıt başarısız')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Layout>
      <div className="auth-grid">
        <section className="auth-hero">
          <p className="eyebrow">Yeni hesap</p>
          <h1>User rolü ile kayıt olun</h1>
          <p>Yeni kullanıcılar User rolü alır. Admin hesabı seed ile gelir.</p>
        </section>
        <form className="card auth-card" onSubmit={onSubmit}>
          <h2>Kayıt ol</h2>
          {error && <div className="alert alert-error" role="alert">{error}</div>}
          <label>
            Ad Soyad
            <input required minLength={2} value={fullName} onChange={(e) => setFullName(e.target.value)} />
          </label>
          <label>
            E-posta
            <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          <label>
            Şifre
            <input
              type="password"
              required
              minLength={8}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
          <button className="btn btn-primary btn-block" disabled={loading} type="submit">
            {loading ? 'Oluşturuluyor…' : 'Hesap oluştur'}
          </button>
          <p className="auth-switch">
            Zaten hesabınız var mı? <Link to="/login">Giriş yapın</Link>
          </p>
        </form>
      </div>
    </Layout>
  )
}
