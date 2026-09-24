import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Layout } from '../components/Layout'

export function SettingsPage() {
  const { token } = useAuth()
  const [host, setHost] = useState('')
  const [port, setPort] = useState(587)
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [hasPassword, setHasPassword] = useState(false)
  const [fromEmail, setFromEmail] = useState('')
  const [fromName, setFromName] = useState('Ticket Board')
  const [enableSsl, setEnableSsl] = useState(true)
  const [enabled, setEnabled] = useState(false)
  const [testTo, setTestTo] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!token) return
    api.getSmtp(token).then((settings) => {
      setHost(settings.host)
      setPort(settings.port || 587)
      setUsername(settings.username)
      setHasPassword(settings.hasPassword)
      setFromEmail(settings.fromEmail)
      setFromName(settings.fromName || 'Ticket Board')
      setEnableSsl(settings.enableSsl)
      setEnabled(settings.enabled)
    }).catch((err) => {
      setError(err instanceof ApiError ? err.message : 'Ayarlar yüklenemedi')
    })
  }, [token])

  async function onSave(e: FormEvent) {
    e.preventDefault()
    if (!token) return
    setSaving(true)
    setError(null)
    setMessage(null)
    try {
      const saved = await api.saveSmtp(token, {
        host,
        port,
        username,
        password: password || undefined,
        fromEmail,
        fromName,
        enableSsl,
        enabled,
      })
      setHasPassword(saved.hasPassword)
      setPassword('')
      setMessage('SMTP ayarları kaydedildi')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kayıt başarısız')
    } finally {
      setSaving(false)
    }
  }

  async function onTest() {
    if (!token) return
    setError(null)
    setMessage(null)
    try {
      await api.testSmtp(token, testTo.trim())
      setMessage('Test e-postası gönderildi')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Test gönderilemedi')
    }
  }

  return (
    <Layout>
      <div className="page-head">
        <div>
          <p className="eyebrow">Admin</p>
          <h1>E-posta bildirimleri</h1>
          <p className="muted">
            Bu form deneme içindir. Normal kullanım için SMTP kurulumunu ortam dosyasında (.env) yapın.
            Bildirimler önce .env ayarını kullanır; Host boşsa buradaki kayıt kullanılır. Şifre ekranda tekrar gösterilmez.
          </p>
        </div>
      </div>
      {error && <div className="alert alert-error">{error}</div>}
      {message && <div className="alert alert-ok">{message}</div>}
      <form className="card settings-form" onSubmit={onSave}>
        <label className="check-line">
          <input type="checkbox" checked={enabled} onChange={(e) => setEnabled(e.target.checked)} />
          Bildirim e-postalarını gönder
        </label>
        <div className="form-row">
          <label>
            Sunucu
            <input value={host} onChange={(e) => setHost(e.target.value)} placeholder="smtp.example.com" />
          </label>
          <label>
            Port
            <input type="number" min={1} max={65535} value={port} onChange={(e) => setPort(Number(e.target.value))} />
          </label>
        </div>
        <div className="form-row">
          <label>
            Kullanıcı adı
            <input value={username} onChange={(e) => setUsername(e.target.value)} />
          </label>
          <label>
            Şifre
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={hasPassword ? 'Kayıtlı şifre duruyor' : 'SMTP şifresi'}
              autoComplete="new-password"
            />
          </label>
        </div>
        <div className="form-row">
          <label>
            Gönderen e-posta
            <input type="email" value={fromEmail} onChange={(e) => setFromEmail(e.target.value)} />
          </label>
          <label>
            Gönderen adı
            <input value={fromName} onChange={(e) => setFromName(e.target.value)} />
          </label>
        </div>
        <label className="check-line">
          <input type="checkbox" checked={enableSsl} onChange={(e) => setEnableSsl(e.target.checked)} />
          SSL / STARTTLS kullan
        </label>
        <button className="btn btn-primary" type="submit" disabled={saving}>
          {saving ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
      </form>
      <form
        className="card settings-form"
        onSubmit={(e) => {
          e.preventDefault()
          void onTest()
        }}
      >
        <h2>Test gönder</h2>
        <label>
          Alıcı
          <input type="email" required value={testTo} onChange={(e) => setTestTo(e.target.value)} placeholder="admin@ticket.local" />
        </label>
        <button className="btn" type="submit">Test e-postası gönder</button>
      </form>
    </Layout>
  )
}
