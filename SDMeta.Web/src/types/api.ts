export type QuerySortBy =
  | 'Newest'
  | 'Oldest'
  | 'Largest'
  | 'Smallest'
  | 'AtoZ'
  | 'ZtoA'
  | 'Random'

export type GroupByMode = 'none' | 'prompt'

export interface ApiError {
  code: string
  message: string
  details?: unknown
}

export interface ImageListItem {
  imageId: string
  fileName: string
  fullPromptHash: string | null
  thumbnailUrl: string
  contentUrl: string
}

export interface PromptGroup {
  fullPromptHash: string
  count: number
  items: ImageListItem[]
}

export interface ImageListResponse {
  items: ImageListItem[] | null
  groups: PromptGroup[] | null
  nextCursor: string | null
  totalApprox: number
}

export interface ParsedPrompt {
  positive: string | null
  negative: string | null
  parameters: string | null
  warnings: string | null
}

export interface ImageDetailResponse {
  imageId: string
  fileName: string
  lastUpdatedUtc: string
  lengthBytes: number
  promptFormat: 'None' | 'Auto1111' | 'ComfyUI'
  promptRaw: string | null
  promptParsed: ParsedPrompt | null
  model: string | null
  modelHash: string | null
  promptHash: string | null
  negativePromptHash: string | null
  exists: boolean
}

export interface ModelResponseItem {
  model: string | null
  modelHash: string | null
  count: number
  label: string
}

export interface StartScanResponse {
  scanId: string
}

export interface PartialScanRequest {
  added?: string[]
  removed?: string[]
  usePendingWatcherQueue?: boolean
}

export type ScanStatus = 'Queued' | 'Running' | 'Completed' | 'Failed'

export interface ScanStateResponse {
  scanId: string
  type: string
  status: ScanStatus
  progress: number
  addedCount: number
  removedCount: number
  startedUtc: string
  completedUtc: string | null
  error: string | null
  revision: number
}

export interface PendingChangesResponse {
  addedCount: number
  removedCount: number
}

export interface StorageSettingsResponse {
  imageDirs: string[]
  thumbnailDir: string
  dbPath: string
  dbSizeBytes: number | null
}

export interface GalleryQueryState {
  filter: string
  model: string
  modelHash: string
  sortBy: QuerySortBy
  groupBy: GroupByMode
}
