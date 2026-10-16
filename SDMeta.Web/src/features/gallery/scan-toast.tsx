import { useEffect, useRef } from 'react'
import { toast } from 'sonner'
import type { ScanStateResponse } from '../../types/api'

interface ScanToastProps {
  scanState: ScanStateResponse | null
}

export function ScanToast({ scanState }: ScanToastProps) {
  const lastStatusRef = useRef<ScanStateResponse['status'] | null>(null)

  useEffect(() => {
    const status = scanState?.status ?? null
    const previousStatus = lastStatusRef.current

    if (status === previousStatus) {
      return
    }

    lastStatusRef.current = status

    if (status === 'Completed') {
      toast.success('Scan completed')
    } else if (status === 'Failed') {
      toast.error('Scan failed')
    }
  }, [scanState?.status])

  return null
}
