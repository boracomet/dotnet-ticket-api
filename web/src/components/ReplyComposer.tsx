import { useState } from 'react'
import type { FormEvent } from 'react'
import { MarkdownEditor } from './MarkdownEditor'

export function ReplyComposer({
  sending,
  onSubmit,
}: {
  sending: boolean
  onSubmit: (body: string, files: File[]) => Promise<void>
}) {
  const [body, setBody] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [error, setError] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (!body.trim() && files.length === 0) return
    setError(null)
    try {
      await onSubmit(body.trim(), files)
      setBody('')
      setFiles([])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Cevap gönderilemedi')
    }
  }

  return (
    <form className="reply-compose" onSubmit={submit}>
      <MarkdownEditor
        value={body}
        onChange={setBody}
        files={files}
        onFilesChange={setFiles}
        placeholder="Cevabınızı yazın. **kalın**, *italik*, liste kullanabilirsiniz."
      />
      {error && <p className="form-error">{error}</p>}
      <button className="btn btn-primary" type="submit" disabled={sending || (!body.trim() && files.length === 0)}>
        {sending ? 'Gönderiliyor…' : 'Cevabı gönder'}
      </button>
    </form>
  )
}
