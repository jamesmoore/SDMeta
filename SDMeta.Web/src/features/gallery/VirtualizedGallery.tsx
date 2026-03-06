import { useEffect, useMemo, useRef } from 'react'
import { useVirtualizer } from '@tanstack/react-virtual'
import { Badge } from '../../components/ui/badge'
import { GAP, TILE_HEIGHT, TOP_ROW_PADDING, type GalleryCard, type VirtualRow } from './gallery-model'

interface VirtualizedGalleryProps {
  rows: VirtualRow[]
  columnCount: number
  loading: boolean
  fetchingNextPage: boolean
  selectedImageId: string | null
  expandedGroupId: string | null
  onSelectImage: (imageId: string) => void
  onToggleGroup: (card: GalleryCard) => void
  onLoadMore: () => void
  onViewportWidthChange: (width: number) => void
}

export function VirtualizedGallery({
  rows,
  columnCount,
  loading,
  fetchingNextPage,
  selectedImageId,
  expandedGroupId,
  onSelectImage,
  onToggleGroup,
  onLoadMore,
  onViewportWidthChange,
}: VirtualizedGalleryProps) {
  const galleryRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    const element = galleryRef.current
    if (!element) return

    const observer = new ResizeObserver((entries) => {
      const entry = entries[0]
      if (entry) {
        onViewportWidthChange(entry.contentRect.width)
      }
    })

    observer.observe(element)
    return () => observer.disconnect()
  }, [onViewportWidthChange])

  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => galleryRef.current,
    estimateSize: (index) => {
      const row = rows[index]
      if (!row) return TILE_HEIGHT + GAP
      if (row.kind === 'expanded') {
        const rowCount = Math.ceil(row.cards.length / Math.max(columnCount, 1))
        return TOP_ROW_PADDING + rowCount * (TILE_HEIGHT + GAP) + 20
      }
      return TILE_HEIGHT + GAP
    },
    overscan: 8,
  })

  const virtualRows = rowVirtualizer.getVirtualItems()

  useEffect(() => {
    if (virtualRows.length === 0) return
    const lastVisible = virtualRows[virtualRows.length - 1]
    if (lastVisible && lastVisible.index >= rows.length - 4) {
      onLoadMore()
    }
  }, [onLoadMore, rows.length, virtualRows])

  const totalSize = useMemo(() => rowVirtualizer.getTotalSize(), [rowVirtualizer])

  return (
    <main ref={galleryRef} className="flex-1 overflow-auto bg-zinc-600 px-2 pb-28">
      {loading ? (
        <div className="grid grid-cols-2 gap-3 pt-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
          {Array.from({ length: 24 }).map((_, index) => (
            <div key={index} className="h-44 animate-pulse rounded-md bg-zinc-500" />
          ))}
        </div>
      ) : (
        <div style={{ height: `${totalSize}px`, position: 'relative' }}>
          {virtualRows.map((virtualRow) => {
            const row = rows[virtualRow.index]
            if (!row) return null

            return (
              <div
                key={row.id}
                data-index={virtualRow.index}
                ref={rowVirtualizer.measureElement}
                className="absolute left-0 w-full pb-3"
                style={{ transform: `translateY(${virtualRow.start}px)` }}
              >
                {row.kind === 'grid' ? (
                  <div className="grid" style={{ gap: `${GAP}px`, gridTemplateColumns: `repeat(${columnCount}, minmax(0, 1fr))` }}>
                    {row.cards.map((card) => {
                      const isSelected = selectedImageId === card.imageId
                      const countBadgeVisible = card.isRepresentative && (card.groupCount ?? 0) > 1

                      return (
                        <button
                          key={card.imageId}
                          type="button"
                          className={`relative overflow-hidden rounded-md border text-left transition ${
                            isSelected
                              ? 'border-sky-400 ring-2 ring-sky-400/80'
                              : card.isRepresentative
                                ? 'border-zinc-300/40 hover:border-zinc-200'
                                : 'border-zinc-300/20 hover:border-zinc-300/50'
                          }`}
                          onClick={() => {
                            if (card.isRepresentative) {
                              onToggleGroup(card)
                            } else {
                              onSelectImage(card.imageId)
                            }
                          }}
                        >
                          <img src={card.thumbnailUrl} alt={card.fileName} loading="lazy" className="h-44 w-full object-cover" />
                          {countBadgeVisible && <Badge className="absolute bottom-1 left-1">{card.groupCount}</Badge>}
                          {card.isRepresentative && expandedGroupId === card.groupKey && (
                            <div className="absolute inset-0 border-2 border-sky-400" />
                          )}
                        </button>
                      )
                    })}
                  </div>
                ) : (
                  <div className="my-1 rounded-md border border-sky-300/35 bg-zinc-700/80 p-2">
                    <div className="grid" style={{ gap: `${GAP}px`, gridTemplateColumns: `repeat(${columnCount}, minmax(0, 1fr))` }}>
                      {row.cards.map((card) => {
                        const isSelected = selectedImageId === card.imageId
                        return (
                          <button
                            key={`${row.groupKey}-${card.imageId}`}
                            type="button"
                            className={`overflow-hidden rounded-md border transition ${
                              isSelected ? 'border-sky-400 ring-2 ring-sky-400/80' : 'border-sky-300/40 hover:border-sky-200/70'
                            }`}
                            onClick={() => onSelectImage(card.imageId)}
                          >
                            <img src={card.thumbnailUrl} alt={card.fileName} loading="lazy" className="h-44 w-full object-cover" />
                          </button>
                        )
                      })}
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {fetchingNextPage && <div className="py-4 text-center text-sm text-zinc-200">Loading more images...</div>}
    </main>
  )
}
