import { useRef, useState } from 'react'
import { renderMarkdown, wrapSelection } from '../utils/markdown'

const ACCEPT = '.pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png'
const MAX_FILES = 3
const MAX_BYTES = 5 * 1024 * 1024

const TOOLS = [
  { label: 'B', title: 'Kalın', before: '**', after: '**' },
  { label: 'I', title: 'İtalik', before: '*', after: '*' },
  { label: '</>', title: 'Kod', before: '`', after: '`' },
  { label: 'Link', title: 'Bağlantı', before: '[', after: '](https://)' },
  { label: 'Liste', title: 'Liste', before: '- ', after: '' },
]

export function MarkdownEditor({
  value,
  onChange,
  files,
  onFilesChange,
  placeholder,
  rows = 4,
}: {
  value: string
  onChange: (value: string) => void
  files: File[]
  onFilesChange: (files: File[]) => void
  placeholder: string
  rows?: number
}) {
  const area = useRef<HTMLTextAreaElement>(null)
  const [preview, setPreview] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function apply(before: string, after: string) {
    const el = area.current
    const start = el?.selectionStart ?? value.length
    const end = el?.selectionEnd ?? value.length
    const next = wrapSelection(value, start, end, before, after)
    onChange(next.value)
    requestAnimationFrame(() => {
      el?.focus()
      el?.setSelectionRange(next.start, next.end)
    })
  }

  function onFiles(list: FileList | null) {
    if (!list) return
    const next = [...files]
    for (const file of Array.from(list)) {
      const ext = file.name.split('.').pop()?.toLowerCase()
      if (!ext || !['pdf', 'jpg', 'jpeg', 'png'].includes(ext)) {
        setError('Yalnızca PDF, JPG ve PNG yüklenebilir')
        return
      }
      if (file.size > MAX_BYTES) {
        setError('Dosya 5 MB sınırını aşıyor')
        return
      }
      if (next.length >= MAX_FILES) {
        setError('En fazla 3 dosya eklenebilir')
        return
      }
      next.push(file)
    }
    setError(null)
    onFilesChange(next)
  }

  return (
    <div className="md-editor">
      <div className="md-toolbar">
        {TOOLS.map((tool) => (
          <button
            key={tool.label}
            type="button"
            className="md-btn"
            title={tool.title}
            onClick={() => apply(tool.before, tool.after)}
          >
            {tool.label}
          </button>
        ))}
        <button type="button" className={`md-btn ${preview ? 'is-on' : ''}`} onClick={() => setPreview((v) => !v)}>
          Önizleme
        </button>
      </div>
      {preview ? (
        <div
          className="md-preview"
          dangerouslySetInnerHTML={{ __html: renderMarkdown(value || 'Önizleme boş') }}
        />
      ) : (
        <textarea
          ref={area}
          rows={rows}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
        />
      )}
      <div className="file-row">
        <label className="btn file-pick">
          Dosya ekle
          <input
            type="file"
            accept={ACCEPT}
            multiple
            onChange={(e) => {
              onFiles(e.target.files)
              e.target.value = ''
            }}
          />
        </label>
        <span className="muted">PDF, JPG, PNG · en fazla 3 dosya</span>
      </div>
      {files.length > 0 && (
        <ul className="file-chips">
          {files.map((file, index) => (
            <li key={`${file.name}-${index}`}>
              <span>{file.name}</span>
              <button type="button" onClick={() => onFilesChange(files.filter((_, i) => i !== index))}>
                kaldır
              </button>
            </li>
          ))}
        </ul>
      )}
      {error && <p className="form-error">{error}</p>}
    </div>
  )
}
