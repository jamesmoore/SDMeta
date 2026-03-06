import { Loader2, RefreshCw, Settings, X } from 'lucide-react'
import type { Dispatch, SetStateAction } from 'react'
import { Badge } from '../../components/ui/badge'
import { Button } from '../../components/ui/button'
import { Input } from '../../components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select'
import { Switch } from '../../components/ui/switch'
import type { GalleryQueryState, ModelResponseItem, PendingChangesResponse, QuerySortBy } from '../../types/api'
import { combineModel, splitModel } from './gallery-model'

const SORT_OPTIONS: QuerySortBy[] = ['Newest', 'Oldest', 'Largest', 'Smallest', 'AtoZ', 'ZtoA', 'Random']

interface GalleryToolbarProps {
  queryState: GalleryQueryState
  filterInput: string
  setFilterInput: Dispatch<SetStateAction<string>>
  setQueryState: (patch: Partial<GalleryQueryState>) => void
  models: ModelResponseItem[]
  itemCount: number
  pendingCounts?: PendingChangesResponse
  autoRescan: boolean
  setAutoRescan: Dispatch<SetStateAction<boolean>>
  onRescan: () => void
  scanBusy: boolean
  scanProgress: number
  onOpenSettings: () => void
  onResetExpandedGroup: () => void
}

export function GalleryToolbar({
  queryState,
  filterInput,
  setFilterInput,
  setQueryState,
  models,
  itemCount,
  pendingCounts,
  autoRescan,
  setAutoRescan,
  onRescan,
  scanBusy,
  scanProgress,
  onOpenSettings,
  onResetExpandedGroup,
}: GalleryToolbarProps) {
  const selectedModelValue = combineModel(queryState.model, queryState.modelHash)

  return (
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
            {models.map((model) => (
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
              onResetExpandedGroup()
              setQueryState({ groupBy: checked ? 'prompt' : 'none' })
            }}
          />
          <span className="text-sm text-zinc-100">Group by prompt</span>
        </div>

        <Button variant="secondary" disabled={scanBusy} onClick={onRescan}>
          {scanBusy ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
          Rescan
        </Button>

        <div className="flex items-center gap-2 px-1">
          <Switch checked={autoRescan} onCheckedChange={setAutoRescan} />
          <span className="text-sm text-zinc-100">Auto rescan</span>
        </div>

        <div className="justify-self-end text-right text-sm text-zinc-100">
          {itemCount.toLocaleString()} items
          {pendingCounts && (pendingCounts.addedCount > 0 || pendingCounts.removedCount > 0) && (
            <div className="mt-1 flex justify-end gap-2">
              {pendingCounts.addedCount > 0 && <Badge variant="secondary">+{pendingCounts.addedCount}</Badge>}
              {pendingCounts.removedCount > 0 && <Badge variant="destructive">-{pendingCounts.removedCount}</Badge>}
            </div>
          )}
        </div>

        <Button variant="ghost" size="icon" title="Settings" onClick={onOpenSettings}>
          <Settings className="h-4 w-4" />
        </Button>
      </div>

      {scanProgress > 0 && (
        <div className="mt-2 h-1 overflow-hidden rounded bg-zinc-800">
          <div className="h-full bg-sky-500 transition-all" style={{ width: `${scanProgress}%` }} />
        </div>
      )}
    </header>
  )
}
