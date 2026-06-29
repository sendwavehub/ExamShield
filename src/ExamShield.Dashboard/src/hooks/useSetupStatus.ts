import { useState, useEffect } from 'react'
import { api } from '../api/client'
import type { SetupStatusResponse } from '../api/client'

export function useSetupStatus() {
  const [status, setStatus] = useState<SetupStatusResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = async () => {
    try {
      setLoading(true)
      setError(null)
      const s = await api.getSetupStatus()
      setStatus(s)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Cannot reach API')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { refresh() }, [])

  return { status, loading, error, refresh }
}
