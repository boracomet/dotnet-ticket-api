import type {
  AuthResponse,
  ErrorResponse,
  NotificationList,
  PagedResult,
  SmtpSettings,
  TicketPriority,
  TicketReplyResponse,
  TicketResponse,
  TicketStatus,
} from './types'

const API_BASE = import.meta.env.VITE_API_BASE ?? ''

export class ApiError extends Error {
  code: string
  status: number
  constructor(status: number, code: string, message: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string | null,
): Promise<T> {
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string> | undefined),
  }
  if (!(options.body instanceof FormData)) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })
  if (res.status === 204) return undefined as T

  const text = await res.text()
  let body: unknown = null
  if (text) {
    try {
      body = JSON.parse(text)
    } catch {
      body = { message: text }
    }
  }

  if (!res.ok) {
    const err = body as ErrorResponse | null
    throw new ApiError(
      res.status,
      err?.code ?? 'ERROR',
      err?.message ?? `İstek başarısız (${res.status})`,
    )
  }
  return body as T
}

export interface TicketListParams {
  page?: number
  pageSize?: number
  status?: TicketStatus | ''
  priority?: TicketPriority | ''
  search?: string
}

export const api = {
  register: (email: string, fullName: string, password: string) =>
    request<AuthResponse>('/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, fullName, password }),
    }),

  login: (email: string, password: string) =>
    request<AuthResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  listTickets: (token: string, params: TicketListParams) => {
    const q = new URLSearchParams()
    q.set('page', String(params.page ?? 1))
    q.set('pageSize', String(params.pageSize ?? 10))
    if (params.status) q.set('status', params.status)
    if (params.priority) q.set('priority', params.priority)
    if (params.search?.trim()) q.set('search', params.search.trim())
    return request<PagedResult<TicketResponse>>(`/api/v1/tickets?${q}`, {}, token)
  },

  createTicket: (
    token: string,
    title: string,
    description: string,
    priority: TicketPriority,
    files: File[] = [],
  ) => {
    const form = new FormData()
    form.append('title', title)
    form.append('description', description)
    form.append('priority', priority)
    for (const file of files) form.append('files', file)
    return request<TicketResponse>('/api/v1/tickets', { method: 'POST', body: form }, token)
  },

  fetchTicketFile: async (token: string, ticketId: string, fileId: string) => {
    const res = await fetch(`${API_BASE}/api/v1/tickets/${ticketId}/files/${fileId}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (!res.ok) throw new ApiError(res.status, 'ERROR', 'Dosya açılamadı')
    return res.blob()
  },

  getTicket: (token: string, id: string) =>
    request<TicketResponse>(`/api/v1/tickets/${id}`, {}, token),

  listReplies: (token: string, id: string) =>
    request<TicketReplyResponse[]>(`/api/v1/tickets/${id}/replies`, {}, token),

  addReply: (token: string, id: string, body: string, files: File[]) => {
    const form = new FormData()
    form.append('body', body)
    for (const file of files) form.append('files', file)
    return request<TicketReplyResponse>(`/api/v1/tickets/${id}/replies`, { method: 'POST', body: form }, token)
  },

  fetchFile: async (token: string, ticketId: string, replyId: string, fileId: string) => {
    const res = await fetch(
      `${API_BASE}/api/v1/tickets/${ticketId}/replies/${replyId}/files/${fileId}`,
      { headers: { Authorization: `Bearer ${token}` } },
    )
    if (!res.ok) throw new ApiError(res.status, 'ERROR', 'Dosya açılamadı')
    return res.blob()
  },

  listNotifications: (token: string) =>
    request<NotificationList>('/api/v1/notifications', {}, token),

  markNotificationRead: (token: string, id: string) =>
    request<void>(`/api/v1/notifications/${id}/read`, { method: 'POST' }, token),

  getSmtp: (token: string) => request<SmtpSettings>('/api/v1/admin/smtp', {}, token),

  saveSmtp: (
    token: string,
    settings: Omit<SmtpSettings, 'hasPassword'> & { password?: string },
  ) =>
    request<SmtpSettings>(
      '/api/v1/admin/smtp',
      { method: 'PUT', body: JSON.stringify(settings) },
      token,
    ),

  testSmtp: (token: string, toEmail: string) =>
    request<void>('/api/v1/admin/smtp/test', { method: 'POST', body: JSON.stringify({ toEmail }) }, token),

  changeStatus: (token: string, id: string, status: TicketStatus) =>
    request<TicketResponse>(
      `/api/v1/tickets/${id}/status`,
      { method: 'PATCH', body: JSON.stringify({ status }) },
      token,
    ),

  deleteTicket: (token: string, id: string) =>
    request<void>(`/api/v1/tickets/${id}`, { method: 'DELETE' }, token),
}
