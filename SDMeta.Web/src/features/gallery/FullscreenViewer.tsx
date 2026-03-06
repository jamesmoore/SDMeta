import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '../../components/ui/button'
import { Dialog, DialogContent } from '../../components/ui/dialog'
import type { ImageListItem } from '../../types/api'

interface FullscreenViewerProps {
  open: boolean
  setOpen: (value: boolean) => void
  selectedItem: ImageListItem | null
  onPrev: () => void
  onNext: () => void
}

export function FullscreenViewer({ open, setOpen, selectedItem, onPrev, onNext }: FullscreenViewerProps) {
  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent className="max-w-[95vw] bg-zinc-950 p-2 text-zinc-100">
        {selectedItem && (
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" onClick={onPrev}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <img src={selectedItem.contentUrl} alt={selectedItem.fileName} className="max-h-[85vh] w-full object-contain" />
            <Button variant="ghost" size="icon" onClick={onNext}>
              <ChevronRight className="h-5 w-5" />
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
