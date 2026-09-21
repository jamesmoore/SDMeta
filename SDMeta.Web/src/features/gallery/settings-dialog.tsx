import { Button } from '../../components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '../../components/ui/dialog'
import { formatBytes } from '../../lib/api'
import type { StorageSettingsResponse } from '../../types/api'

interface SettingsDialogProps {
  open: boolean
  setOpen: (value: boolean) => void
  storage: StorageSettingsResponse | undefined
  clearThumbs: () => void
  clearDb: () => void
  clearingThumbs: boolean
  clearingDb: boolean
}

export function SettingsDialog({
  open,
  setOpen,
  storage,
  clearThumbs,
  clearDb,
  clearingThumbs,
  clearingDb,
}: SettingsDialogProps) {
  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent className="max-w-3xl bg-neutral-900 text-neutral-100">
        <DialogHeader>
          <DialogTitle>Settings</DialogTitle>
          <DialogDescription>Storage and cache maintenance</DialogDescription>
        </DialogHeader>

        <div className="space-y-4 text-sm">
          <div className="grid grid-cols-[160px_1fr_auto] gap-2 border-b border-neutral-800 pb-3">
            <div className="text-neutral-400">Thumbnails</div>
            <div className="break-all">{storage?.thumbnailDir ?? '-'}</div>
            <Button variant="secondary" size="sm" onClick={clearThumbs} disabled={clearingThumbs}>
              Clear
            </Button>
          </div>

          <div className="grid grid-cols-[160px_1fr_auto] gap-2 border-b border-neutral-800 pb-3">
            <div className="text-neutral-400">Prompt database</div>
            <div>
              <div className="break-all">{storage?.dbPath ?? '-'}</div>
              <div className="text-neutral-400">
                {storage?.dbSizeBytes != null ? formatBytes(storage.dbSizeBytes) : 'Does not exist'}
              </div>
            </div>
            <Button variant="destructive" size="sm" onClick={clearDb} disabled={clearingDb}>
              Clear
            </Button>
          </div>

          <div className="grid grid-cols-[160px_1fr] gap-2">
            <div className="text-neutral-400">Image directories</div>
            <ul className="list-inside list-disc space-y-1">
              {(storage?.imageDirs ?? []).map((path) => (
                <li key={path} className="break-all">
                  {path}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
