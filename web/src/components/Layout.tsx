import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { NotificationBell } from './NotificationBell'

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout, isAdmin } = useAuth()

  return (
    <div className="app-shell">
      <header className="topbar">
        <Link to="/" className="brand">
          <span className="brand-mark">🎫</span>
          <span>
            Ticket Board
            <small>.NET 8 · JWT · Roller</small>
          </span>
        </Link>
        {user && (
          <div className="topbar-right">
            <NotificationBell />
            {isAdmin && (
              <Link className="btn" to="/settings">
                Ayarlar
              </Link>
            )}
            <span className={`role-badge ${isAdmin ? 'role-admin' : 'role-user'}`}>
              {isAdmin ? 'Admin' : 'User'}
            </span>
            <div className="user-chip">
              <span className="avatar">{user.fullName.charAt(0).toUpperCase()}</span>
              <div>
                <strong>{user.fullName}</strong>
                <small>{user.email}</small>
              </div>
            </div>
            <button type="button" className="btn btn-ghost" onClick={logout}>
              Çıkış
            </button>
          </div>
        )}
      </header>
      <main className="main">{children}</main>
      <footer className="footer">
        Portfolio case study · Bora Ata Türkoğlu · durum workflow: Open → InProgress → Resolved → Closed
      </footer>
    </div>
  )
}
