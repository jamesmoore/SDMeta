import { AlertTriangle, Check } from 'lucide-react'
import type { ScanStateResponse } from '../../types/api'

interface ScanToastProps {
  scanState: ScanStateResponse | null
}

export function ScanToast({ scanState }: ScanToastProps) {
  if (!scanState) return null

  if (scanState.status === 'Completed') {
    return (
      <div className="fixed bottom-32 right-3 rounded-md border border-emerald-300/50 bg-emerald-900/70 px-3 py-2 text-xs text-emerald-100">
        <div className="flex items-center gap-1">
          <Check className="h-3.5 w-3.5" />
          Scan completed
        </div>
      </div>
    )
  }

  if (scanState.status === 'Failed') {
    return (
      <div className="fixed bottom-32 right-3 rounded-md border border-red-300/50 bg-red-900/70 px-3 py-2 text-xs text-red-100">
        <div className="flex items-center gap-1">
          <AlertTriangle className="h-3.5 w-3.5" />
          Scan failed
        </div>
      </div>
    )
  }

  return null
}
