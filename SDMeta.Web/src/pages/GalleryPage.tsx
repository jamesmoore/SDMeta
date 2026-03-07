import { AlertTriangle } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useGalleryData } from '../hooks/useGalleryData'
import { useGalleryQueryState } from '../hooks/useGalleryQueryState'
import { useScanLifecycle } from '../hooks/useScanLifecycle'
import type { ImageDetailResponse } from '../types/api'
import { GAP, TILE_WIDTH, buildRows, type GalleryCard, type PagedImages } from '../features/gallery/gallery-model'
import { FullscreenViewer } from '../features/gallery/FullscreenViewer'
import { GalleryToolbar } from '../features/gallery/GalleryToolbar'
import { ImageDetailsFooter, type FooterSelection } from '../features/gallery/ImageDetailsFooter'
import { ScanToast } from '../features/gallery/ScanToast'
import { SettingsDialog } from '../features/gallery/SettingsDialog'
import { VirtualizedGallery } from '../features/gallery/VirtualizedGallery'

export function GalleryPage() {
  const [viewportWidth, setViewportWidth] = useState(1200)
  const [filterInput, setFilterInput] = useState('')
  const [selectedImageId, setSelectedImageId] = useState<string | null>(null)
  const [expandedGroupId, setExpandedGroupId] = useState<string | null>(null)
  const [isFullscreenOpen, setIsFullscreenOpen] = useState(false)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const [autoRescan, setAutoRescan] = useState(false)
  const [footerSelection, setFooterSelection] = useState<FooterSelection | null>(null)

  const debouncedFilter = useDebouncedValue(filterInput, 450)
  const { queryState, setQueryState } = useGalleryQueryState()

  useEffect(() => {
    setFilterInput(queryState.filter)
  }, [queryState.filter])

  useEffect(() => {
    if (debouncedFilter !== queryState.filter) {
      setQueryState({ filter: debouncedFilter })
    }
  }, [debouncedFilter, queryState.filter, setQueryState])

  const columnCount = useMemo(() => Math.max(1, Math.floor((viewportWidth + GAP) / (TILE_WIDTH + GAP))), [viewportWidth])

  const galleryData = useGalleryData({
    queryState,
    selectedImageId,
    isSettingsOpen,
    autoRescan,
    onClearSelection: () => setSelectedImageId(null),
  })

  const scan = useScanLifecycle({
    autoRescan,
    pendingChanges: galleryData.pendingQuery.data,
    onInvalidateAfterScan: galleryData.invalidateAfterScan,
  })

  const rowsData = useMemo(
    () => buildRows(queryState.groupBy, galleryData.imageQuery.data as PagedImages, columnCount, expandedGroupId),
    [queryState.groupBy, galleryData.imageQuery.data, columnCount, expandedGroupId],
  )

  const allItems = rowsData.navigationItems

  const selectedItem = useMemo(
    () => (selectedImageId ? allItems.find((item) => item.imageId === selectedImageId) ?? null : null),
    [allItems, selectedImageId],
  )

  const detail = galleryData.detailQuery.data

  useEffect(() => {
    if (!selectedImageId) {
      setFooterSelection(null)
      return
    }

    if (!selectedItem || !detail) {
      return
    }

    if (detail.imageId !== selectedItem.imageId) {
      return
    }

    setFooterSelection((current) => {
      if (current?.item.imageId === selectedItem.imageId && current.detail.imageId === detail.imageId) {
        return current
      }

      return {
        item: selectedItem,
        detail,
      }
    })
  }, [selectedImageId, selectedItem, detail])

  const isLoadingNextDetail =
    selectedImageId !== null &&
    galleryData.detailQuery.isFetching &&
    footerSelection !== null &&
    footerSelection.item.imageId !== selectedImageId

  const selectAdjacent = (delta: number) => {
    if (!selectedImageId) return
    const index = allItems.findIndex((item) => item.imageId === selectedImageId)
    if (index < 0) return

    const nextIndex = Math.min(Math.max(index + delta, 0), allItems.length - 1)
    const next = allItems[nextIndex]
    if (next) setSelectedImageId(next.imageId)
  }

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (!isFullscreenOpen) return
      if (event.key === 'ArrowLeft') {
        event.preventDefault()
        selectAdjacent(-1)
      } else if (event.key === 'ArrowRight') {
        event.preventDefault()
        selectAdjacent(1)
      }
    }

    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  })

  const loading = galleryData.imageQuery.isLoading || galleryData.modelsQuery.isLoading

  const onToggleGroup = (card: GalleryCard) => {
    if (!card.groupKey) return
    setExpandedGroupId(card.groupKey === expandedGroupId ? null : card.groupKey)
  }

  const onCopyPrompt = async (detail: ImageDetailResponse) => {
    if (!detail.promptRaw) return
    await navigator.clipboard.writeText(detail.promptRaw)
  }

  const onLoadMore = () => {
    if (!galleryData.imageQuery.hasNextPage) return
    if (galleryData.imageQuery.isFetchingNextPage) return
    galleryData.imageQuery.fetchNextPage().catch(() => undefined)
  }

  return (
    <div className="flex h-full flex-col bg-neutral-700 text-neutral-100">
      <GalleryToolbar
        queryState={queryState}
        filterInput={filterInput}
        setFilterInput={setFilterInput}
        setQueryState={setQueryState}
        models={galleryData.modelsQuery.data ?? []}
        itemCount={rowsData.totalApprox}
        pendingCounts={galleryData.pendingQuery.data}
        autoRescan={autoRescan}
        setAutoRescan={setAutoRescan}
        onRescan={scan.startFullScan}
        scanBusy={scan.scanBusy}
        scanProgress={scan.scanProgress}
        onOpenSettings={() => setIsSettingsOpen(true)}
        onResetExpandedGroup={() => setExpandedGroupId(null)}
      />

      {galleryData.imageQuery.error && (
        <div className="px-3 pt-2 text-sm text-red-300">
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-4 w-4" />
            {(galleryData.imageQuery.error as Error).message}
          </div>
        </div>
      )}

      <VirtualizedGallery
        rows={rowsData.rows}
        columnCount={columnCount}
        loading={loading}
        fetchingNextPage={galleryData.imageQuery.isFetchingNextPage}
        selectedImageId={selectedImageId}
        expandedGroupId={expandedGroupId}
        onSelectImage={setSelectedImageId}
        onToggleGroup={onToggleGroup}
        onLoadMore={onLoadMore}
        onViewportWidthChange={setViewportWidth}
      />

      <ImageDetailsFooter
        selection={footerSelection}
        isLoadingNext={isLoadingNextDetail}
        deleting={galleryData.deleteMutation.isPending}
        onDelete={(imageId) => galleryData.deleteMutation.mutate(imageId)}
        onOpenFullscreen={() => setIsFullscreenOpen(true)}
        onCopyPrompt={onCopyPrompt}
      />

      <FullscreenViewer
        open={isFullscreenOpen}
        setOpen={setIsFullscreenOpen}
        selectedItem={selectedItem}
        onPrev={() => selectAdjacent(-1)}
        onNext={() => selectAdjacent(1)}
      />

      <SettingsDialog
        open={isSettingsOpen}
        setOpen={setIsSettingsOpen}
        storage={galleryData.storageQuery.data}
        clearThumbs={() => galleryData.clearThumbsMutation.mutate()}
        clearDb={() => galleryData.clearDbMutation.mutate()}
        clearingThumbs={galleryData.clearThumbsMutation.isPending}
        clearingDb={galleryData.clearDbMutation.isPending}
      />

      <ScanToast scanState={scan.scanState} />
    </div>
  )
}
