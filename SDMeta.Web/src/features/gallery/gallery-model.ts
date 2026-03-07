import type { GroupByMode, ImageListItem, ImageListResponse } from '../../types/api'

export const TILE_WIDTH = 176
export const TILE_HEIGHT = 176
export const GAP = 12
export const TOP_ROW_PADDING = 8

export type GalleryCard = ImageListItem & {
  isRepresentative: boolean
  groupCount?: number
  groupItems?: ImageListItem[]
  groupKey?: string
}

export type GridRow = {
  id: string
  kind: 'grid'
  cards: GalleryCard[]
}

export type ExpandedRow = {
  id: string
  kind: 'expanded'
  cards: ImageListItem[]
  groupKey: string
}

export type VirtualRow = GridRow | ExpandedRow

export type PagedImages = { pages: ImageListResponse[] } | undefined

export function chunk<T>(items: T[], size: number): T[][] {
  if (size <= 0) return [items]

  const result: T[][] = []
  for (let i = 0; i < items.length; i += size) {
    result.push(items.slice(i, i + size))
  }
  return result
}

export function combineModel(model: string, modelHash: string): string {
  return model ? `${model}||${modelHash}` : '__all__'
}

export function splitModel(value: string): { model: string; modelHash: string } {
  if (!value || value === '__all__') {
    return { model: '', modelHash: '' }
  }

  const [model, modelHash = ''] = value.split('||')
  return { model, modelHash }
}

export function buildRows(
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

export function formatDate(value: string): string {
  return new Date(value).toLocaleString()
}

export function promptLineColor(line: string): string {
  if (line.startsWith('Negative prompt:')) return 'text-sky-300'
  if (line.includes('<lora:') || line.includes('<hypernet:')) return 'text-emerald-300'
  if (line.includes('Model hash:') || line.includes('Seed:')) return 'text-sky-300'
  return 'text-neutral-200'
}
