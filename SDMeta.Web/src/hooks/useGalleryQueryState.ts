import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { GalleryQueryState, GroupByMode, QuerySortBy } from '../types/api'

const DEFAULT_STATE: GalleryQueryState = {
  filter: '',
  model: '',
  modelHash: '',
  sortBy: 'Newest',
  groupBy: 'none',
}

const SORT_VALUES: QuerySortBy[] = ['Newest', 'Oldest', 'Largest', 'Smallest', 'AtoZ', 'ZtoA', 'Random']

function parseSortBy(value: string | null): QuerySortBy {
  if (value && SORT_VALUES.includes(value as QuerySortBy)) {
    return value as QuerySortBy
  }
  return DEFAULT_STATE.sortBy
}

function parseGroupBy(value: string | null): GroupByMode {
  return value === 'prompt' ? 'prompt' : 'none'
}

function parseQuery(params: URLSearchParams): GalleryQueryState {
  return {
    filter: params.get('filter') ?? DEFAULT_STATE.filter,
    model: params.get('model') ?? DEFAULT_STATE.model,
    modelHash: params.get('modelHash') ?? DEFAULT_STATE.modelHash,
    sortBy: parseSortBy(params.get('sortBy')),
    groupBy: parseGroupBy(params.get('groupBy')),
  }
}

function toSearchParams(state: GalleryQueryState): URLSearchParams {
  const params = new URLSearchParams()

  if (state.filter) params.set('filter', state.filter)
  if (state.model) params.set('model', state.model)
  if (state.modelHash) params.set('modelHash', state.modelHash)
  if (state.sortBy !== DEFAULT_STATE.sortBy) params.set('sortBy', state.sortBy)
  if (state.groupBy !== DEFAULT_STATE.groupBy) params.set('groupBy', state.groupBy)

  return params
}

export function useGalleryQueryState() {
  const [searchParams, setSearchParams] = useSearchParams()

  const queryState = useMemo<GalleryQueryState>(() => parseQuery(searchParams), [searchParams])

  const setQueryState = useCallback(
    (patch: Partial<GalleryQueryState>) => {
      setSearchParams((prev) => {
        const nextState = { ...parseQuery(prev), ...patch }
        return toSearchParams(nextState)
      }, { replace: true })
    },
    [setSearchParams],
  )

  return { queryState, setQueryState }
}
