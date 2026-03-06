import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useVirtualizer } from '@tanstack/react-virtual'
import {
  AlertTriangle,
  Check,
  ChevronLeft,
  ChevronRight,
  Copy,
  Expand,
  Loader2,
  RefreshCw,
  Settings,
  Trash2,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { api, formatBytes, subscribeToScanEvents } from './lib/api'
import { useDebouncedValue } from './hooks/useDebouncedValue'
import { useGalleryQueryState } from './hooks/useGalleryQueryState'
import type {
  GroupByMode,
  ImageDetailResponse,
  ImageListItem,
  ImageListResponse,
  ModelResponseItem,
  QuerySortBy,
  ScanStateResponse,
} from './types/api'
import { Badge } from './components/ui/badge'
import { Button } from './components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from './components/ui/dialog'
import { Input } from './components/ui/input'
import { ScrollArea } from './components/ui/scroll-area'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './components/ui/select'
import { Separator } from './components/ui/separator'
import { Switch } from './components/ui/switch'

type GalleryCard = ImageListItem & {
  isRepresentative: boolean
  groupCount?: number
  groupItems?: ImageListItem[]
  groupKey?: string
}

type GridRow = {
  id: string
  kind: 'grid'
  cards: GalleryCard[]
}

type ExpandedRow = {
  id: string
  kind: 'expanded'
  cards: ImageListItem[]
  groupKey: string
}

type VirtualRow = GridRow | ExpandedRow

type PagedImages = { pages: ImageListResponse[] } | undefined

const TILE_WIDTH = 176
const TILE_HEIGHT = 176
const GAP = 12
const TOP_ROW_PADDING = 8
const SCAN_BUSY: ScanStateResponse['status'][] = ['Queued', 'Running']
const SORT_OPTIONS: QuerySortBy[] = ['Newest', 'Oldest', 'Largest', 'Smallest', 'AtoZ', 'ZtoA', 'Random']

function chunk<T>(items: T[], size: number): T[][] {
  if (size <= 0) return [items]

  const result: T[][] = []
  for (let i = 0; i < items.length; i += size) {
    result.push(items.slice(i, i + size))
  }
  return result
}

function combineModel(model: string, modelHash: string): string {
  return model ? `${model}||${modelHash}` : '__all__'
}

function splitModel(value: string): { model: string; modelHash: string } {
  if (!value || value === '__all__') {
    return { model: '', modelHash: '' }
  }

  const [model, modelHash = ''] = value.split('||')
  return { model, modelHash }
}

function buildRows(
  mode: GroupByMode,
  pagesData: PagedImages,
  columnCount: number,
  expandedGroupId: string | null,
): { rows: VirtualRow[]; navigationItems: ImageListItem[]; totalApprox: number } {
  const safeColumnCount = Math.max(columnCount, 1)
  const allPages = pagesData?.pages ?? []
  const totalApprox = allPages.at(-1)?.totalApprox ?? 0

  if (mode === 'prompt') {
    const groups = allPages.flatMap((page) => page.groups ?? [])

    const reps: GalleryCard[] = groups
      .filter((group) => group.items.length > 0)
      .map((group) => {
        const representative = group.items[0]
        const groupKey = `${group.fullPromptHash}::${representative.imageId}`
        return {
          ...representative,
          isRepresentative: true,
          groupCount: group.count,
          groupItems: group.items,
          groupKey,
        }
      })

    const repRows = chunk(reps, safeColumnCount)
    const rows: VirtualRow[] = []

    repRows.forEach((cards, index) => {
      rows.push({ id: `grid-${index}`, kind: 'grid', cards })

      if (!expandedGroupId) return

      const expandedSource = cards.find((card) => card.groupKey === expandedGroupId)
      if (!expandedSource?.groupItems) return

      rows.push({
        id: `expanded-${expandedSource.groupKey}`,
        kind: 'expanded',
        cards: expandedSource.groupItems,
        groupKey: expandedSource.groupKey ?? expandedGroupId,
      })
    })

    const navigationItems = groups.flatMap((group) => group.items)
    return { rows, navigationItems, totalApprox }
  }

  const items = allPages.flatMap((page) => page.items ?? [])
  const rows = chunk(items, safeColumnCount).map<VirtualRow>((cards, index) => ({
    id: `grid-${index}`,
    kind: 'grid',
    cards: cards.map((card) => ({ ...card, isRepresentative: false })),
  }))

  return { rows, navigationItems: items, totalApprox }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString()
}

function promptLineColor(line: string): string {
  if (line.startsWith('Negative prompt:')) return 'text-sky-300'
  if (line.includes('<lora:') || line.includes('<hypernet:')) return 'text-emerald-300'
  if (line.includes('Model hash:') || line.includes('Seed:')) return 'text-sky-300'
  return 'text-zinc-200'
}

function App() {
  const queryClient = useQueryClient()
  const galleryRef = useRef<HTMLDivElement | null>(null)
  const [viewportWidth, setViewportWidth] = useState(1200)
  const [filterInput, setFilterInput] = useState('')
  const debouncedFilter = useDebouncedValue(filterInput, 450)

  const [selectedImageId, setSelectedImageId] = useState<string | null>(null)
  const [expandedGroupId, setExpandedGroupId] = useState<string | null>(null)
  const [isFullscreenOpen, setIsFullscreenOpen] = useState(false)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const [autoRescan, setAutoRescan] = useState(false)
  const [activeScanId, setActiveScanId] = useState<string | null>(null)
  const [scanState, setScanState] = useState<ScanStateResponse | null>(null)

  const { queryState, setQueryState } = useGalleryQueryState()

  useEffect(() => {
    setFilterInput(queryState.filter)
  }, [queryState.filter])

  useEffect(() => {
    if (debouncedFilter !== queryState.filter) {
      setQueryState({ filter: debouncedFilter })
    }
  }, [debouncedFilter, queryState.filter, setQueryState])

  useEffect(() => {
    const element = galleryRef.current
    if (!element) return

    const observer = new ResizeObserver((entries) => {
      const entry = entries[0]
      if (entry) {
        setViewportWidth(entry.contentRect.width)
      }
    })

    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  const columnCount = useMemo(() => Math.max(1, Math.floor((viewportWidth + GAP) / (TILE_WIDTH + GAP))), [viewportWidth])

  const imageQuery = useInfiniteQuery({
    queryKey: ['images', queryState],
    queryFn: ({ pageParam }) => api.getImages(queryState, pageParam, 100),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  })

  const modelsQuery = useQuery({
    queryKey: ['models'],
    queryFn: api.getModels,
  })

  const pendingQuery = useQuery({
    queryKey: ['pending-changes'],
    queryFn: api.getPendingChanges,
    refetchInterval: autoRescan ? 2000 : false,
  })

  const storageQuery = useQuery({
    queryKey: ['storage'],
    queryFn: api.getStorage,
    enabled: isSettingsOpen,
  })

  const detailQuery = useQuery({
    queryKey: ['image-detail', selectedImageId],
    queryFn: () => api.getImageDetail(selectedImageId!),
    enabled: selectedImageId !== null,
  })

  const rowsData = useMemo(
    () => buildRows(queryState.groupBy, imageQuery.data as PagedImages, columnCount, expandedGroupId),
    [columnCount, expandedGroupId, imageQuery.data, queryState.groupBy],
  )

  const rows = rowsData.rows

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
    if (!imageQuery.hasNextPage || imageQuery.isFetchingNextPage || virtualRows.length === 0) return
    const lastVisible = virtualRows[virtualRows.length - 1]
    if (lastVisible && lastVisible.index >= rows.length - 4) {
      imageQuery.fetchNextPage().catch(() => undefined)
    }
  }, [imageQuery, rows.length, virtualRows])

  const fullScanMutation = useMutation({
    mutationFn: api.startFullScan,
    onSuccess: (data) => {
      setActiveScanId(data.scanId)
      setScanState(null)
    },
  })

  const partialScanMutation = useMutation({
    mutationFn: api.startPartialScan,
    onSuccess: (data) => {
      setActiveScanId(data.scanId)
      setScanState(null)
    },
  })

  const clearThumbsMutation = useMutation({
    mutationFn: api.clearThumbnails,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage'] }),
  })

  const clearDbMutation = useMutation({
    mutationFn: api.clearDatabase,
    onSuccess: async () => {
      setSelectedImageId(null)
      await queryClient.invalidateQueries({ queryKey: ['images'] })
      await queryClient.invalidateQueries({ queryKey: ['storage'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: api.deleteImage,
    onSuccess: async () => {
      setSelectedImageId(null)
      await queryClient.invalidateQueries({ queryKey: ['images'] })
      await queryClient.invalidateQueries({ queryKey: ['pending-changes'] })
    },
  })

  useEffect(() => {
    if (!activeScanId) return

    let unsubscribe: (() => void) | null = null
    let pollTimer: ReturnType<typeof setInterval> | null = null

    const stopPolling = () => {
      if (pollTimer) {
        clearInterval(pollTimer)
        pollTimer = null
      }
    }

    const startPolling = () => {
      if (pollTimer) return

      pollTimer = setInterval(async () => {
        try {
          const next = await api.getScan(activeScanId)
          setScanState(next)

          if (next.status === 'Completed' || next.status === 'Failed') {
            stopPolling()
            setActiveScanId(null)
            await queryClient.invalidateQueries({ queryKey: ['images'] })
            await queryClient.invalidateQueries({ queryKey: ['pending-changes'] })
          }
        } catch {
          stopPolling()
        }
      }, 1200)
    }

    unsubscribe = subscribeToScanEvents(
      activeScanId,
      async (event) => {
        setScanState(event)
        if (event.status === 'Completed' || event.status === 'Failed') {
          setActiveScanId(null)
          stopPolling()
          await queryClient.invalidateQueries({ queryKey: ['images'] })
          await queryClient.invalidateQueries({ queryKey: ['pending-changes'] })
        }
      },
      startPolling,
    )

    return () => {
      unsubscribe?.()
      stopPolling()
    }
  }, [activeScanId, queryClient])

  useEffect(() => {
    if (!autoRescan) return
    if (!pendingQuery.data) return
    if (scanState && SCAN_BUSY.includes(scanState.status)) return

    const hasPending = pendingQuery.data.addedCount > 0 || pendingQuery.data.removedCount > 0
    if (hasPending) {
      partialScanMutation.mutate({ usePendingWatcherQueue: true })
    }
  }, [autoRescan, partialScanMutation, pendingQuery.data, scanState])

  const allItems = rowsData.navigationItems

  const selectedItem = useMemo(
    () => (selectedImageId ? allItems.find((item) => item.imageId === selectedImageId) ?? null : null),
    [allItems, selectedImageId],
  )

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

  const selectedModelValue = combineModel(queryState.model, queryState.modelHash)

  const loading = imageQuery.isLoading || modelsQuery.isLoading
  const itemCount = rowsData.totalApprox

  const onSelectRepresentative = (card: GalleryCard) => {
    if (!card.groupKey) return
    setExpandedGroupId(card.groupKey === expandedGroupId ? null : card.groupKey)
  }

  const onCopyPrompt = async (detail: ImageDetailResponse) => {
    if (!detail.promptRaw) return
    await navigator.clipboard.writeText(detail.promptRaw)
  }

  const scanProgress = scanState?.status === 'Running' ? scanState.progress : 0

  return (
    <div className="flex h-full flex-col bg-zinc-700 text-zinc-100">
      <header className="sticky top-0 z-20 border-b border-zinc-700 bg-zinc-900/95 px-3 py-2 backdrop-blur">
        <div className="grid grid-cols-1 gap-2 md:grid-cols-[minmax(220px,320px)_minmax(200px,300px)_minmax(160px,190px)_auto_auto_auto_1fr_auto] md:items-center">
          <div className="relative">
            <Input
              value={filterInput}
              onChange={(event) => setFilterInput(event.target.value)}
              placeholder="filter"
              className="bg-zinc-100 pr-9 text-zinc-950"
            />
            {filterInput.length > 0 && (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="absolute right-0 top-0 h-10 w-10 text-zinc-600 hover:text-zinc-900"
                onClick={() => setFilterInput('')}
                title="Clear filter"
              >
                <X className="h-4 w-4" />
              </Button>
            )}
          </div>

          <Select
            value={selectedModelValue}
            onValueChange={(value) => {
              const modelSplit = splitModel(value)
              setQueryState({ model: modelSplit.model, modelHash: modelSplit.modelHash })
            }}
          >
            <SelectTrigger className="bg-zinc-100 text-zinc-900">
              <SelectValue placeholder="-- all models --" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">-- all models --</SelectItem>
              {(modelsQuery.data ?? []).map((model: ModelResponseItem) => (
                <SelectItem
                  key={`${model.modelHash ?? 'none'}-${model.model ?? 'none'}`}
                  value={combineModel(model.model ?? '', model.modelHash ?? '')}
                >
                  {model.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={queryState.sortBy} onValueChange={(value) => setQueryState({ sortBy: value as QuerySortBy })}>
            <SelectTrigger className="bg-zinc-100 text-zinc-900">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {SORT_OPTIONS.map((sortBy) => (
                <SelectItem key={sortBy} value={sortBy}>
                  {sortBy}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <div className="flex items-center gap-2 px-1">
            <Switch
              checked={queryState.groupBy === 'prompt'}
              onCheckedChange={(checked) => {
                setExpandedGroupId(null)
                setQueryState({ groupBy: checked ? 'prompt' : 'none' })
              }}
            />
            <span className="text-sm text-zinc-100">Group by prompt</span>
          </div>

          <Button
            variant="secondary"
            disabled={fullScanMutation.isPending || partialScanMutation.isPending}
            onClick={() => fullScanMutation.mutate()}
          >
            {fullScanMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
            Rescan
          </Button>

          <div className="flex items-center gap-2 px-1">
            <Switch checked={autoRescan} onCheckedChange={setAutoRescan} />
            <span className="text-sm text-zinc-100">Auto rescan</span>
          </div>

          <div className="justify-self-end text-right text-sm text-zinc-100">
            {itemCount.toLocaleString()} items
            {pendingQuery.data && (pendingQuery.data.addedCount > 0 || pendingQuery.data.removedCount > 0) && (
              <div className="mt-1 flex justify-end gap-2">
                {pendingQuery.data.addedCount > 0 && <Badge variant="secondary">+{pendingQuery.data.addedCount}</Badge>}
                {pendingQuery.data.removedCount > 0 && <Badge variant="destructive">-{pendingQuery.data.removedCount}</Badge>}
              </div>
            )}
          </div>

          <Dialog open={isSettingsOpen} onOpenChange={setIsSettingsOpen}>
            <DialogTrigger asChild>
              <Button variant="ghost" size="icon" title="Settings">
                <Settings className="h-4 w-4" />
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-3xl bg-zinc-900 text-zinc-100">
              <DialogHeader>
                <DialogTitle>Settings</DialogTitle>
                <DialogDescription>Storage and cache maintenance</DialogDescription>
              </DialogHeader>

              <div className="space-y-4 text-sm">
                <div className="grid grid-cols-[160px_1fr_auto] gap-2 border-b border-zinc-800 pb-3">
                  <div className="text-zinc-400">Thumbnails</div>
                  <div className="break-all">{storageQuery.data?.thumbnailDir ?? '-'}</div>
                  <Button variant="secondary" size="sm" onClick={() => clearThumbsMutation.mutate()} disabled={clearThumbsMutation.isPending}>
                    Clear
                  </Button>
                </div>

                <div className="grid grid-cols-[160px_1fr_auto] gap-2 border-b border-zinc-800 pb-3">
                  <div className="text-zinc-400">Prompt database</div>
                  <div>
                    <div className="break-all">{storageQuery.data?.dbPath ?? '-'}</div>
                    <div className="text-zinc-400">
                      {storageQuery.data?.dbSizeBytes != null ? formatBytes(storageQuery.data.dbSizeBytes) : 'Does not exist'}
                    </div>
                  </div>
                  <Button variant="destructive" size="sm" onClick={() => clearDbMutation.mutate()} disabled={clearDbMutation.isPending}>
                    Clear
                  </Button>
                </div>

                <div className="grid grid-cols-[160px_1fr] gap-2">
                  <div className="text-zinc-400">Image directories</div>
                  <ul className="list-inside list-disc space-y-1">
                    {(storageQuery.data?.imageDirs ?? []).map((path) => (
                      <li key={path} className="break-all">
                        {path}
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </DialogContent>
          </Dialog>
        </div>

        {scanProgress > 0 && (
          <div className="mt-2 h-1 overflow-hidden rounded bg-zinc-800">
            <div className="h-full bg-sky-500 transition-all" style={{ width: `${scanProgress}%` }} />
          </div>
        )}

        {imageQuery.error && (
          <div className="mt-2 flex items-center gap-2 text-sm text-red-300">
            <AlertTriangle className="h-4 w-4" />
            {(imageQuery.error as Error).message}
          </div>
        )}
      </header>

      <main ref={galleryRef} className="flex-1 overflow-auto bg-zinc-600 px-2 pb-28">
        {loading ? (
          <div className="grid grid-cols-2 gap-3 pt-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
            {Array.from({ length: 24 }).map((_, index) => (
              <div key={index} className="h-44 animate-pulse rounded-md bg-zinc-500" />
            ))}
          </div>
        ) : (
          <div style={{ height: `${rowVirtualizer.getTotalSize()}px`, position: 'relative' }}>
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
                                onSelectRepresentative(card)
                              } else {
                                setSelectedImageId(card.imageId)
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
                              onClick={() => setSelectedImageId(card.imageId)}
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

        {imageQuery.isFetchingNextPage && <div className="py-4 text-center text-sm text-zinc-200">Loading more images...</div>}
      </main>

      {detailQuery.data && selectedItem && (
        <footer className="fixed bottom-0 left-0 right-0 z-30 border-t border-zinc-800 bg-zinc-900/95 px-3 py-2 backdrop-blur">
          <div className="flex gap-3">
            <button
              type="button"
              className="h-28 w-28 shrink-0 overflow-hidden rounded border border-zinc-600"
              onClick={() => setIsFullscreenOpen(true)}
            >
              <img src={selectedItem.thumbnailUrl} alt={detailQuery.data.fileName} className="h-full w-full object-cover" />
            </button>

            <div className="min-w-0 flex-1">
              <div className="truncate text-sm text-zinc-100">{detailQuery.data.fileName}</div>
              <div className="mt-1 text-xs text-zinc-300">
                Size: {formatBytes(detailQuery.data.lengthBytes)} | Date: {formatDate(detailQuery.data.lastUpdatedUtc)} | Format:{' '}
                {detailQuery.data.promptFormat}
              </div>

              <Separator className="my-2 bg-zinc-700" />

              <ScrollArea className="h-16 w-full rounded border border-zinc-700 bg-zinc-950 px-2 py-1 font-mono text-xs">
                {(detailQuery.data.promptRaw ?? '')
                  .split('\n')
                  .filter((line) => line.trim().length > 0)
                  .map((line, index) => (
                    <div key={`${index}-${line.slice(0, 8)}`} className={promptLineColor(line)}>
                      {line}
                    </div>
                  ))}
              </ScrollArea>

              <div className="mt-2 flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => onCopyPrompt(detailQuery.data)}
                  disabled={!detailQuery.data.promptRaw}
                >
                  <Copy className="mr-1 h-3.5 w-3.5" />
                  Copy
                </Button>
                <Button variant="secondary" size="sm" onClick={() => setIsFullscreenOpen(true)}>
                  <Expand className="mr-1 h-3.5 w-3.5" />
                  View
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  className="ml-auto"
                  onClick={() => deleteMutation.mutate(detailQuery.data.imageId)}
                  disabled={deleteMutation.isPending}
                >
                  <Trash2 className="mr-1 h-3.5 w-3.5" />
                  Delete
                </Button>
              </div>
            </div>
          </div>
        </footer>
      )}

      <Dialog open={isFullscreenOpen} onOpenChange={setIsFullscreenOpen}>
        <DialogContent className="max-w-[95vw] bg-zinc-950 p-2 text-zinc-100">
          {selectedItem && (
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="icon" onClick={() => selectAdjacent(-1)}>
                <ChevronLeft className="h-5 w-5" />
              </Button>
              <img src={selectedItem.contentUrl} alt={selectedItem.fileName} className="max-h-[85vh] w-full object-contain" />
              <Button variant="ghost" size="icon" onClick={() => selectAdjacent(1)}>
                <ChevronRight className="h-5 w-5" />
              </Button>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {scanState?.status === 'Completed' && (
        <div className="fixed bottom-32 right-3 rounded-md border border-emerald-300/50 bg-emerald-900/70 px-3 py-2 text-xs text-emerald-100">
          <div className="flex items-center gap-1">
            <Check className="h-3.5 w-3.5" />
            Scan completed
          </div>
        </div>
      )}
    </div>
  )
}

export default App
