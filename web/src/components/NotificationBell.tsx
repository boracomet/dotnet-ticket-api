import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { NotificationList } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { formatDate } from '../utils/ticket'

const empty: NotificationList = { unreadCount: 0, items: [] }

export function NotificationBell() {
  const { token } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [data, setData] = useState<NotificationList>(empty)

  const load = useCallback(async () => {
    if (!token) return
    try {
      setData(await api.listNotifications(token))
    } catch {
      /* Bildirim paneli açıkken geçici ağ hatası listeyi bozmasın. */
    }
  }, [token])

  useEffect(() => {
    void load()
    const timer = window.setInterval(() => void load(), 8000)
    return () => window.clearInterval(timer)
  }, [load])

  async function openItem(id: string, ticketId: string) {
    if (!token) return
    setOpen(false)
    try {
      await api.markNotificationRead(token, id)
    } catch {
      /* Okundu işareti başarısız olsa da talebe gidilir. */
    }
    await load()
    navigate(`/tickets/${ticketId}`)
  }

  return (
    <div className="notify">
      <button type="button" className="btn notify-btn" onClick={() => setOpen((v) => !v)}>
        Bildirimler
        {data.unreadCount > 0 && <span className="notify-count">{data.unreadCount}</span>}
      </button>
      {open && (
        <div className="notify-panel">
          {data.items.length === 0 ? (
            <p className="muted">Bildirim yok</p>
          ) : (
            data.items.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`notify-item ${item.isRead ? '' : 'is-unread'}`}
                onClick={() => void openItem(item.id, item.ticketId)}
              >
                <strong>{item.message}</strong>
                <span>{formatDate(item.createdAtUtc)}</span>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}
