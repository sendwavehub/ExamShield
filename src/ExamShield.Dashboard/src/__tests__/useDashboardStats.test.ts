import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: { getDashboardStats: vi.fn() },
}))

import { api } from '../api/client'
import { useDashboardStats } from '../hooks/useDashboardStats'

const mockApi = api as { getDashboardStats: ReturnType<typeof vi.fn> }

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockStats = {
  totalCaptures: 1200,
  verifiedCaptures: 1100,
  pendingOcr: 50,
  activeExams: 3,
}

describe('useDashboardStats', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns stats data on success', async () => {
    mockApi.getDashboardStats.mockResolvedValue(mockStats)
    const { result } = renderHook(() => useDashboardStats(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockStats)
  })

  it('starts in loading state', () => {
    mockApi.getDashboardStats.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useDashboardStats(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getDashboardStats.mockRejectedValue(new Error('server error'))
    const { result } = renderHook(() => useDashboardStats(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })

  it('calls getDashboardStats exactly once on mount', async () => {
    mockApi.getDashboardStats.mockResolvedValue(mockStats)
    renderHook(() => useDashboardStats(), { wrapper })
    await waitFor(() => expect(mockApi.getDashboardStats).toHaveBeenCalledTimes(1))
  })
})
