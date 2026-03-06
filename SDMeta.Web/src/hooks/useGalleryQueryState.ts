import { useMemo } from 'react'
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

export function useGalleryQueryState() {
  const [searchParams, setSearchParams] = useSearchParams()

  const queryState = useMemo<GalleryQueryState>(() => ({
    filter: searchParams.get('filter') ?? DEFAULT_STATE.filter,
    model: searchParams.get('model') ?? DEFAULT_STATE.model,
    modelHash: searchParams.get('modelHash') ?? DEFAULT_STATE.modelHash,
    sortBy: parseSortBy(searchParams.get('sortBy')),
    groupBy: parseGroupBy(searchParams.get('groupBy')),
  }), [searchParams])

  const setQueryState = (patch: Partial<GalleryQueryState>) => {
    const next = { ...queryState, ...patch }
    const params = new URLSearchParams()

    if (next.filter) params.set('filter', next.filter)
    if (next.model) params.set('model', next.model)
    if (next.modelHash) params.set('modelHash', next.modelHash)
    if (next.sortBy !== DEFAULT_STATE.sortBy) params.set('sortBy', next.sortBy)
    if (next.groupBy !== DEFAULT_STATE.groupBy) params.set('groupBy', next.groupBy)

    setSearchParams(params, { replace: true })
  }

  return { queryState, setQueryState }
}
