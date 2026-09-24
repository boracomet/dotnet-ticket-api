import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError, api } from '../api/client'
import type { TicketPriority, TicketResponse, TicketStatus } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { Layout } from '../components/Layout'
import { MarkdownEditor } from '../components/MarkdownEditor'
import {
  PRIORITY_LABELS,
  STATUS_LABELS,
  formatDate,
} from '../utils/ticket'

const PAGE_SIZE = 8

export function BoardPage() {
  const { token, user, isAdmin } = useAuth()
  const navigate = useNavigate()
  const [items, setItems] = useState<TicketResponse[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [status, setStatus] = useState<TicketStatus | ''>('')
  const [priority, setPriority] = useState<TicketPriority | ''>('')
  const [search, setSearch] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionMsg, setActionMsg] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [newPriority, setNewPriority] = useState<TicketPriority>('Medium')
  const [files, setFiles] = useState<File[]>([])
  const [creating, setCreating] = useState(false)

  const load = useCallback(async () => {
    if (!token) return
    setLoading(true)
    setError(null)
    try {
      const res = await api.listTickets(token, {
        page,
        pageSize: PAGE_SIZE,
        status,
        priority,
        search,
      })
      setItems(res.items)
      setTotalPages(Math.max(1, res.totalPages || 1))
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Ticketlar yüklenemedi')
    } finally {
      setLoading(false)
    }
  }, [token, page, status, priority, search])

  useEffect(() => {
    void load()
  }, [load])

  async function onCreate(e: FormEvent) {
    e.preventDefault()
    if (!token) return
    setCreating(true)
    setActionMsg(null)
    try {
      await api.createTicket(token, title.trim(), description.trim(), newPriority, files)
      setTitle('')
      setDescription('')
      setFiles([])
      setNewPriority('Medium')
      setShowCreate(false)
      setActionMsg('Ticket oluşturuldu')
      setPage(1)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Oluşturma başarısız')
    } finally {
      setCreating(false)
    }
  }

  async function onDelete(id: string) {
    if (!token) return
    if (!confirm('Bu ticket silinsin mi?')) return
    setActionMsg(null)
    try {
      await api.deleteTicket(token, id)
      setActionMsg('Ticket silindi')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Silme başarısız')
    }
  }

  function applySearch(e: FormEvent) {
    e.preventDefault()
    setPage(1)
    setSearch(searchInput)
  }

  return (
    <Layout>
      <div className="page-head">
        <div>
          <p className="eyebrow">Ticket panosu</p>
          <h1>Talepler</h1>
          <p className="muted">
            {isAdmin
              ? 'Admin: tüm ticketları görürsünüz.'
              : 'User: kendi oluşturduğunuz / size atanan ticketlar.'}
          </p>
        </div>
        {!isAdmin && (
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate((v) => !v)}>
            {showCreate ? 'Formu kapat' : '+ Yeni ticket'}
          </button>
        )}
      </div>

      {error && (
        <div className="alert alert-error" role="alert">
          {error}
          <button type="button" className="linkish" onClick={() => setError(null)}>
            kapat
          </button>
        </div>
      )}
      {actionMsg && <div className="alert alert-ok">{actionMsg}</div>}

      {!isAdmin && showCreate && (
        <form className="card create-card" onSubmit={onCreate}>
          <h2>Yeni ticket</h2>
          <div className="form-row">
            <label>
              Başlık
              <input required value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Giriş hatası" />
            </label>
            <label>
              Öncelik
              <select
                value={newPriority}
                onChange={(e) => setNewPriority(e.target.value as TicketPriority)}
              >
                {(Object.keys(PRIORITY_LABELS) as TicketPriority[]).map((p) => (
                  <option key={p} value={p}>
                    {PRIORITY_LABELS[p]}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <div className="field">
            <span className="field-label">Açıklama</span>
            <MarkdownEditor
              value={description}
              onChange={setDescription}
              files={files}
              onFilesChange={setFiles}
              placeholder="Sorunu detaylı anlatın. **kalın**, *italik*, liste kullanabilirsiniz."
              rows={5}
            />
          </div>
          <button className="btn btn-accent" type="submit" disabled={creating}>
            {creating ? 'Kaydediliyor…' : 'Oluştur'}
          </button>
        </form>
      )}

      <section className="card filters">
        <form className="filter-bar" onSubmit={applySearch}>
          <label>
            Durum
            <select
              value={status}
              onChange={(e) => {
                setPage(1)
                setStatus(e.target.value as TicketStatus | '')
              }}
            >
              <option value="">Tümü</option>
              {(Object.keys(STATUS_LABELS) as TicketStatus[]).map((s) => (
                <option key={s} value={s}>
                  {STATUS_LABELS[s]}
                </option>
              ))}
            </select>
          </label>
          <label>
            Öncelik
            <select
              value={priority}
              onChange={(e) => {
                setPage(1)
                setPriority(e.target.value as TicketPriority | '')
              }}
            >
              <option value="">Tümü</option>
              {(Object.keys(PRIORITY_LABELS) as TicketPriority[]).map((p) => (
                <option key={p} value={p}>
                  {PRIORITY_LABELS[p]}
                </option>
              ))}
            </select>
          </label>
          <label className="grow">
            Ara
            <input
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Başlık / açıklama"
            />
          </label>
          <button className="btn" type="submit">
            Filtrele
          </button>
        </form>
        <p className="muted meta-line">
          Toplam <strong>{totalCount}</strong> kayıt · Sayfa {page}/{totalPages} · Rol:{' '}
          <strong>{user?.role}</strong>
        </p>
      </section>

      {loading ? (
        <div className="ticket-table card" aria-busy="true">
          {[1, 2, 3].map((i) => (
            <div key={i} className="ticket-row ticket-row-skeleton">
              <div className="skeleton" />
              <div className="skeleton" />
              <div className="skeleton skeleton-lg" />
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <div className="card empty">
          <div className="empty-icon">📭</div>
          <h3>Ticket bulunamadı</h3>
          <p>{isAdmin ? 'Filtreleri temizleyin.' : 'Filtreleri temizleyin veya yeni bir ticket oluşturun.'}</p>
        </div>
      ) : (
        <div className="ticket-table card">
          <div className="ticket-row ticket-row-head">
            <span>Durum</span>
            <span>Öncelik</span>
            <span>Talep</span>
            <span>Güncelleme</span>
            <span />
          </div>
          {items.map((t) => {
            const canDelete = isAdmin
            return (
              <article
                key={t.id}
                className="ticket-row"
                role="link"
                tabIndex={0}
                onClick={() => navigate(`/tickets/${t.id}`)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') navigate(`/tickets/${t.id}`)
                }}
              >
                <span className={`pill status-${t.status}`}>{STATUS_LABELS[t.status]}</span>
                <span className={`pill priority-${t.priority}`}>{PRIORITY_LABELS[t.priority]}</span>
                <div className="ticket-row-main">
                  <strong>{t.title}</strong>
                  <span>{t.description || 'Açıklama yok'}</span>
                </div>
                <time dateTime={t.updatedAtUtc}>{formatDate(t.updatedAtUtc)}</time>
                <div className="ticket-row-actions">
                  {canDelete && (
                    <button
                      type="button"
                      className="btn btn-danger-ghost"
                      onClick={(e) => {
                        e.stopPropagation()
                        void onDelete(t.id)
                      }}
                    >
                      Sil
                    </button>
                  )}
                </div>
              </article>
            )
          })}
        </div>
      )}

      <div className="pager">
        <button
          type="button"
          className="btn"
          disabled={page <= 1 || loading}
          onClick={() => setPage((p) => Math.max(1, p - 1))}
        >
          ← Önceki
        </button>
        <span>
          {page} / {totalPages}
        </span>
        <button
          type="button"
          className="btn"
          disabled={page >= totalPages || loading}
          onClick={() => setPage((p) => p + 1)}
        >
          Sonraki →
        </button>
      </div>
    </Layout>
  )
}
