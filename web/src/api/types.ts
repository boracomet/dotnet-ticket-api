export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed'
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface AuthResponse {
  accessToken: string
  tokenType: string
  userId: string
  email: string
  fullName: string
  role: string
}

export interface TicketResponse {
  id: string
  title: string
  description: string
  status: TicketStatus
  priority: TicketPriority
  createdByUserId: string
  assignedToUserId: string | null
  createdAtUtc: string
  updatedAtUtc: string
  attachments: ReplyAttachment[]
}

export interface ReplyAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
}

export interface TicketReplyResponse {
  id: string
  ticketId: string
  authorUserId: string
  authorName: string
  body: string
  createdAtUtc: string
  attachments: ReplyAttachment[]
}

export interface NotificationItem {
  id: string
  ticketId: string
  message: string
  isRead: boolean
  createdAtUtc: string
}

export interface NotificationList {
  unreadCount: number
  items: NotificationItem[]
}

export interface SmtpSettings {
  host: string
  port: number
  username: string
  hasPassword: boolean
  fromEmail: string
  fromName: string
  enableSsl: boolean
  enabled: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ErrorResponse {
  code: string
  message: string
  timestamp?: string
}
