import { useMutation } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api, subscribeToScanEvents } from '../lib/api'
import type { PendingChangesResponse, ScanStateResponse, StartScanResponse } from '../types/api'

const SCAN_BUSY: ScanStateResponse['status'][] = ['Queued', 'Running']
const SCAN_TERMINAL: ScanStateResponse['status'][] = ['Completed', 'Failed']

interface UseScanLifecycleOptions {
  autoRescan: boolean
  pendingChanges: PendingChangesResponse | undefined
  onInvalidateAfterScan: () => Promise<void>
}

export function useScanLifecycle({ autoRescan, pendingChanges, onInvalidateAfterScan }: UseScanLifecycleOptions) {
  const [activeScanId, setActiveScanId] = useState<string | null>(null)
  const [scanState, setScanState] = useState<ScanStateResponse | null>(null)

  const beginScan = (result: StartScanResponse) => {
    setActiveScanId(result.scanId)
    setScanState(null)
  }

  const fullScanMutation = useMutation({
    mutationFn: api.startFullScan,
    onSuccess: beginScan,
  })

  const partialScanMutation = useMutation({
    mutationFn: api.startPartialScan,
    onSuccess: beginScan,
  })

  useEffect(() => {
    if (!activeScanId) return

    let unsubscribe: (() => void) | null = null
    let pollTimer: ReturnType<typeof setInterval> | null = null

    const stopPolling = () => {
      if (!pollTimer) return
      clearInterval(pollTimer)
      pollTimer = null
    }

    const handleTerminalState = async (next: ScanStateResponse) => {
      if (!SCAN_TERMINAL.includes(next.status)) return
      stopPolling()
      setActiveScanId(null)
      await onInvalidateAfterScan()
    }

    const startPolling = () => {
      if (pollTimer) return

      pollTimer = setInterval(async () => {
        try {
          const next = await api.getScan(activeScanId)
          setScanState(next)
          await handleTerminalState(next)
        } catch {
          stopPolling()
        }
      }, 1200)
    }

    unsubscribe = subscribeToScanEvents(
      activeScanId,
      async (event) => {
        setScanState(event)
        await handleTerminalState(event)
      },
      startPolling,
    )

    return () => {
      unsubscribe?.()
      stopPolling()
    }
  }, [activeScanId, onInvalidateAfterScan])

  useEffect(() => {
    if (!autoRescan) return
    if (!pendingChanges) return
    if (scanState && SCAN_BUSY.includes(scanState.status)) return
    if (partialScanMutation.isPending || fullScanMutation.isPending) return

    const hasPending = pendingChanges.addedCount > 0 || pendingChanges.removedCount > 0
    if (!hasPending) return

    partialScanMutation.mutate({ usePendingWatcherQueue: true })
  }, [
    autoRescan,
    pendingChanges,
    scanState,
    partialScanMutation,
    partialScanMutation.isPending,
    fullScanMutation.isPending,
  ])

  return {
    scanState,
    scanBusy:
      fullScanMutation.isPending ||
      partialScanMutation.isPending ||
      (scanState ? SCAN_BUSY.includes(scanState.status) : false),
    scanProgress: scanState?.status === 'Running' ? scanState.progress : 0,
    startFullScan: () => fullScanMutation.mutate(),
    startPartialScan: partialScanMutation.mutate,
    fullScanPending: fullScanMutation.isPending,
    partialScanPending: partialScanMutation.isPending,
  }
}
