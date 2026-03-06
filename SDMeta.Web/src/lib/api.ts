import type {
  ApiError,
  GalleryQueryState,
  ImageDetailResponse,
  ImageListResponse,
  ModelResponseItem,
  PartialScanRequest,
  PendingChangesResponse,
  ScanStateResponse,
  StartScanResponse,
  StorageSettingsResponse,
} from '../types/api'

const API_ROOT = '/api/v1'

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_ROOT}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    let apiError: ApiError | null = null
    try {
      apiError = (await response.json()) as ApiError
    } catch {
      apiError = null
    }

    const error = new Error(apiError?.message ?? response.statusText)
    ;(error as Error & { code?: string; status?: number }).code = apiError?.code
    ;(error as Error & { code?: string; status?: number }).status = response.status
    throw error
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

function toQueryString(query: GalleryQueryState, cursor?: string, limit = 100): string {
  const params = new URLSearchParams()

  if (query.filter) params.set('filter', query.filter)
  if (query.model) params.set('model', query.model)
  if (query.modelHash) params.set('modelHash', query.modelHash)

  params.set('sortBy', query.sortBy)
  params.set('groupBy', query.groupBy)
  params.set('limit', String(limit))
  if (cursor) params.set('cursor', cursor)

  return params.toString()
}

export const api = {
  getImages(query: GalleryQueryState, cursor?: string, limit = 100) {
    return apiFetch<ImageListResponse>(`/images?${toQueryString(query, cursor, limit)}`)
  },
  getImageDetail(imageId: string) {
    return apiFetch<ImageDetailResponse>(`/images/${imageId}`)
  },
  deleteImage(imageId: string) {
    return apiFetch<void>(`/images/${imageId}`, { method: 'DELETE' })
  },
  getModels() {
    return apiFetch<ModelResponseItem[]>('/models')
  },
  startFullScan() {
    return apiFetch<StartScanResponse>('/scans/full', { method: 'POST' })
  },
  startPartialScan(body: PartialScanRequest) {
    return apiFetch<StartScanResponse>('/scans/partial', {
      method: 'POST',
      body: JSON.stringify(body),
    })
  },
  getScan(scanId: string) {
    return apiFetch<ScanStateResponse>(`/scans/${scanId}`)
  },
  getPendingChanges() {
    return apiFetch<PendingChangesResponse>('/scans/pending')
  },
  getStorage() {
    return apiFetch<StorageSettingsResponse>('/settings/storage')
  },
  clearThumbnails() {
    return apiFetch<void>('/settings/cache/thumbnails', { method: 'DELETE' })
  },
  clearDatabase() {
    return apiFetch<void>('/settings/cache/database', { method: 'DELETE' })
  },
}

export function subscribeToScanEvents(
  scanId: string,
  onEvent: (event: ScanStateResponse) => void,
  onError: () => void,
): () => void {
  const eventSource = new EventSource(`${API_ROOT}/scans/${scanId}/events`)

  const handleMessage = (raw: MessageEvent<string>) => {
    try {
      const payload = JSON.parse(raw.data) as ScanStateResponse
      onEvent(payload)
    } catch {
      // Ignore parse failures from transient frames.
    }
  }

  eventSource.onmessage = handleMessage
  eventSource.addEventListener('progress', handleMessage as EventListener)
  eventSource.onerror = () => {
    onError()
    eventSource.close()
  }

  return () => {
    eventSource.close()
  }
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  const units = ['KB', 'MB', 'GB', 'TB']
  let value = bytes
  let index = -1
  do {
    value /= 1024
    index += 1
  } while (value >= 1024 && index < units.length - 1)

  return `${value.toFixed(value >= 10 ? 1 : 2)} ${units[index]}`
}
