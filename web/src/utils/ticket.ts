import type { TicketPriority, TicketStatus } from '../api/types'

export const STATUS_LABELS: Record<TicketStatus, string> = {
  Open: 'Açık',
  InProgress: 'Devam ediyor',
  Resolved: 'Çözüldü',
  Closed: 'Kapalı',
}

export const PRIORITY_LABELS: Record<TicketPriority, string> = {
  Low: 'Düşük',
  Medium: 'Orta',
  High: 'Yüksek',
  Critical: 'Kritik',
}

/** Allowed next statuses from TicketService workflow */
export const ALLOWED_TRANSITIONS: Record<TicketStatus, TicketStatus[]> = {
  Open: ['InProgress', 'Closed'],
  InProgress: ['Resolved', 'Open', 'Closed'],
  Resolved: ['Closed', 'InProgress'],
  Closed: [],
}

export function formatDate(iso: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}
