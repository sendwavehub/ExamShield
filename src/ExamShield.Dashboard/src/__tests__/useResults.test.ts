import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getResults: vi.fn(),
    getStatistics: vi.fn(),
  },
}))

import { api } from '../api/client'
import { useResults, useStatistics } from '../hooks/useResults'

const mockApi = api as {
  getResults: ReturnType<typeof vi.fn>
  getStatistics: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useResults', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns results on success', async () => {
    const mockData = { items: [{ studentId: 'stu-1', score: 85 }], total: 1 }
    mockApi.getResults.mockResolvedValue(mockData)
    const { result } = renderHook(() => useResults(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getResults.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useResults(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getResults.mockRejectedValue(new Error('unauthorized'))
    const { result } = renderHook(() => useResults(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useStatistics', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns statistics on success', async () => {
    const mockStats = { averageScore: 72.5, passRate: 0.88 }
    mockApi.getStatistics.mockResolvedValue(mockStats)
    const { result } = renderHook(() => useStatistics(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockStats)
  })

  it('starts in loading state', () => {
    mockApi.getStatistics.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useStatistics(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getStatistics.mockRejectedValue(new Error('error'))
    const { result } = renderHook(() => useStatistics(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
