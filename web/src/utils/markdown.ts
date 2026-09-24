function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

function inline(value: string): string {
  return value
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
    .replace(/\*([^*]+)\*/g, '<em>$1</em>')
    .replace(
      /\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/g,
      '<a href="$2" target="_blank" rel="noreferrer">$1</a>',
    )
}

export function renderMarkdown(source: string): string {
  const lines = escapeHtml(source).split('\n')
  const html: string[] = []
  let inList = false
  const closeList = () => {
    if (!inList) return
    html.push('</ul>')
    inList = false
  }

  for (const line of lines) {
    const item = line.match(/^\s*[-*]\s+(.*)$/)
    if (item) {
      if (!inList) {
        html.push('<ul>')
        inList = true
      }
      html.push(`<li>${inline(item[1])}</li>`)
      continue
    }
    closeList()
    if (!line.trim()) continue
    html.push(`<p>${inline(line)}</p>`)
  }
  closeList()
  return html.join('')
}

export function wrapSelection(
  value: string,
  start: number,
  end: number,
  before: string,
  after: string,
): { value: string; start: number; end: number } {
  const selected = value.slice(start, end) || 'metin'
  const next = value.slice(0, start) + before + selected + after + value.slice(end)
  const cursor = start + before.length
  return { value: next, start: cursor, end: cursor + selected.length }
}
