import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { ReplyAttachment } from '../api/types'
import { renderMarkdown } from '../utils/markdown'

export function ReplyBody({
  token,
  ticketId,
  replyId,
  body,
  attachments,
}: {
  token: string
  ticketId: string
  replyId: string
  body: string
  attachments: ReplyAttachment[]
}) {
  const load = useCallback(
    (file: ReplyAttachment) => api.fetchFile(token, ticketId, replyId, file.id),
    [token, ticketId, replyId],
  )
  return (
    <>
      {body ? (
        <div className="md-body" dangerouslySetInnerHTML={{ __html: renderMarkdown(body) }} />
      ) : null}
      <AttachmentList files={attachments} load={load} />
    </>
  )
}

export function AttachmentList({
  files,
  load,
}: {
  files: ReplyAttachment[]
  load: (file: ReplyAttachment) => Promise<Blob>
}) {
  if (files.length === 0) return null
  return (
    <div className="attachment-list">
      {files.map((file) =>
        file.contentType.startsWith('image/') ? (
          <AuthImage key={file.id} file={file} load={load} />
        ) : (
          <button
            key={file.id}
            type="button"
            className="btn file-link"
            onClick={() => void download(file, load)}
          >
            {file.fileName}
          </button>
        ),
      )}
    </div>
  )
}

function AuthImage({
  file,
  load,
}: {
  file: ReplyAttachment
  load: (file: ReplyAttachment) => Promise<Blob>
}) {
  const [url, setUrl] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    let objectUrl = ''
    load(file).then((blob) => {
      if (!active) return
      objectUrl = URL.createObjectURL(blob)
      setUrl(objectUrl)
    }).catch(() => {
      if (active) setUrl(null)
    })
    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [file, load])

  if (!url) return <span className="muted">{file.fileName}</span>
  return <img className="reply-image" src={url} alt={file.fileName} />
}

async function download(file: ReplyAttachment, load: (file: ReplyAttachment) => Promise<Blob>) {
  const blob = await load(file)
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = file.fileName
  link.click()
  URL.revokeObjectURL(url)
}
