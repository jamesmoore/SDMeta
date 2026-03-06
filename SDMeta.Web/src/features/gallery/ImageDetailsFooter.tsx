import { Copy, Expand, Trash2 } from 'lucide-react'
import { Button } from '../../components/ui/button'
import { ScrollArea } from '../../components/ui/scroll-area'
import { Separator } from '../../components/ui/separator'
import { formatBytes } from '../../lib/api'
import type { ImageDetailResponse, ImageListItem } from '../../types/api'
import { formatDate, promptLineColor } from './gallery-model'

interface ImageDetailsFooterProps {
  selectedItem: ImageListItem | null
  detail: ImageDetailResponse | undefined
  deleting: boolean
  onDelete: (imageId: string) => void
  onOpenFullscreen: () => void
  onCopyPrompt: (detail: ImageDetailResponse) => void
}

export function ImageDetailsFooter({
  selectedItem,
  detail,
  deleting,
  onDelete,
  onOpenFullscreen,
  onCopyPrompt,
}: ImageDetailsFooterProps) {
  if (!selectedItem || !detail) {
    return null
  }

  return (
    <footer className="fixed bottom-0 left-0 right-0 z-30 border-t border-zinc-800 bg-zinc-900/95 px-3 py-2 backdrop-blur">
      <div className="flex gap-3">
        <button
          type="button"
          className="h-28 w-28 shrink-0 overflow-hidden rounded border border-zinc-600"
          onClick={onOpenFullscreen}
        >
          <img src={selectedItem.thumbnailUrl} alt={detail.fileName} className="h-full w-full object-cover" />
        </button>

        <div className="min-w-0 flex-1">
          <div className="truncate text-sm text-zinc-100">{detail.fileName}</div>
          <div className="mt-1 text-xs text-zinc-300">
            Size: {formatBytes(detail.lengthBytes)} | Date: {formatDate(detail.lastUpdatedUtc)} | Format: {detail.promptFormat}
          </div>

          <Separator className="my-2 bg-zinc-700" />

          <ScrollArea className="h-16 w-full rounded border border-zinc-700 bg-zinc-950 px-2 py-1 font-mono text-xs">
            {(detail.promptRaw ?? '')
              .split('\n')
              .filter((line) => line.trim().length > 0)
              .map((line, index) => (
                <div key={`${index}-${line.slice(0, 8)}`} className={promptLineColor(line)}>
                  {line}
                </div>
              ))}
          </ScrollArea>

          <div className="mt-2 flex items-center gap-2">
            <Button variant="secondary" size="sm" onClick={() => onCopyPrompt(detail)} disabled={!detail.promptRaw}>
              <Copy className="mr-1 h-3.5 w-3.5" />
              Copy
            </Button>
            <Button variant="secondary" size="sm" onClick={onOpenFullscreen}>
              <Expand className="mr-1 h-3.5 w-3.5" />
              View
            </Button>
            <Button
              variant="destructive"
              size="sm"
              className="ml-auto"
              onClick={() => onDelete(detail.imageId)}
              disabled={deleting}
            >
              <Trash2 className="mr-1 h-3.5 w-3.5" />
              Delete
            </Button>
          </div>
        </div>
      </div>
    </footer>
  )
}
