import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError, api } from '../api/client'
import type { ReplyAttachment, TicketReplyResponse, TicketResponse, TicketStatus } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { Layout } from '../components/Layout'
import { AttachmentList, ReplyBody } from '../components/ReplyBody'
import { renderMarkdown } from '../utils/markdown'
import { ReplyComposer } from '../components/ReplyComposer'
import { ALLOWED_TRANSITIONS, PRIORITY_LABELS, STATUS_LABELS, formatDate } from '../utils/ticket'

export function TicketDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { token, isAdmin } = useAuth()
  const [ticket, setTicket] = useState<TicketResponse | null>(null)
  const [replies, setReplies] = useState<TicketReplyResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!token || !id) return
    setLoading(true)
    setError(null)
    try {
      const [item, thread] = await Promise.all([
        api.getTicket(token, id),
        api.listReplies(token, id),
      ])
      setTicket(item)
      setReplies(thread)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Ticket açılamadı')
    } finally {
      setLoading(false)
    }
  }, [token, id])

  useEffect(() => {
    void load()
  }, [load])

  async function onReply(body: string, files: File[]) {
    if (!token || !id) return
    setSending(true)
    setError(null)
    try {
      const created = await api.addReply(token, id, body, files)
      setReplies((current) => [...current, created])
    } catch (err) {
      setSending(false)
      throw err instanceof ApiError ? err : new Error('Cevap gönderilemedi')
    }
    setSending(false)
  }

  async function onStatusChange(next: TicketStatus) {
    if (!token || !id) return
    setError(null)
    try {
      const updated = await api.changeStatus(token, id, next)
      setTicket(updated)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Durum güncellenemedi')
    }
  }

  const loadTicketFile = useCallback(
    (file: ReplyAttachment) => api.fetchTicketFile(token ?? '', id ?? '', file.id),
    [token, id],
  )

  const nextStatuses = ticket ? ALLOWED_TRANSITIONS[ticket.status] : []

  return (
    <Layout>
      <div className="detail-toolbar">
        <Link className="btn" to="/">
          ← Panele dön
        </Link>
      </div>

      {error && (
        <div className="alert alert-error" role="alert">
          {error}
          <button type="button" className="linkish" onClick={() => setError(null)}>
            kapat
          </button>
        </div>
      )}

      {loading ? (
        <div className="card skeleton-card" aria-busy="true">
          <div className="skeleton skeleton-lg" />
          <div className="skeleton" />
          <div className="skeleton skeleton-sm" />
        </div>
      ) : !ticket ? (
        <div className="card empty">
          <h3>Ticket bulunamadı</h3>
          <button type="button" className="btn" onClick={() => navigate('/')}>
            Panele dön
          </button>
        </div>
      ) : (
        <div className="detail-layout">
          <article className="card">
            <div className="ticket-top">
              <span className={`pill status-${ticket.status}`}>{STATUS_LABELS[ticket.status]}</span>
              <span className={`pill priority-${ticket.priority}`}>{PRIORITY_LABELS[ticket.priority]}</span>
            </div>
            <h1 className="detail-title">{ticket.title}</h1>
            {ticket.description ? (
              <div
                className="md-body"
                dangerouslySetInnerHTML={{ __html: renderMarkdown(ticket.description) }}
              />
            ) : (
              !(ticket.attachments ?? []).length && <p className="detail-body">Açıklama yok</p>
            )}
            {token && <AttachmentList files={ticket.attachments ?? []} load={loadTicketFile} />}
            <div className="ticket-meta">
              <span>Oluşturulma: {formatDate(ticket.createdAtUtc)}</span>
              <span>Güncelleme: {formatDate(ticket.updatedAtUtc)}</span>
            </div>
            {isAdmin && (
              <div className="detail-status">
                {nextStatuses.length > 0 ? (
                  <label>
                    Durum
                    <select
                      value=""
                      onChange={(e) => {
                        const value = e.target.value as TicketStatus
                        if (value) void onStatusChange(value)
                      }}
                    >
                      <option value="">Durum değiştir</option>
                      {nextStatuses.map((status) => (
                        <option key={status} value={status}>
                          {STATUS_LABELS[status]}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : (
                  <p className="muted reply-empty">Bu ticket kapalı.</p>
                )}
              </div>
            )}
          </article>

          <section className="card">
            <h2>Cevaplar</h2>
            {replies.length === 0 ? (
              <p className="muted reply-empty">Henüz cevap yok.</p>
            ) : (
              <div className="reply-list">
                {replies.map((reply) => (
                  <article key={reply.id} className="reply">
                    <div className="reply-head">
                      <strong>{reply.authorName}</strong>
                      <span>{formatDate(reply.createdAtUtc)}</span>
                    </div>
                    {token && (
                      <ReplyBody
                        token={token}
                        ticketId={ticket.id}
                        replyId={reply.id}
                        body={reply.body}
                        attachments={reply.attachments ?? []}
                      />
                    )}
                  </article>
                ))}
              </div>
            )}

            <ReplyComposer sending={sending} onSubmit={onReply} />
          </section>
        </div>
      )}
    </Layout>
  )
}
