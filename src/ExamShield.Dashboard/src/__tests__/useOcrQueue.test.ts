import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getOcrQueue: vi.fn(),
    triggerOcr: vi.fn(),
    triggerBatchOcr: vi.fn(),
  },
}))

import { api } from '../api/client'
import { useOcrQueue, useTriggerOcr, useBatchOcr } from '../hooks/useOcrQueue'

const mockApi = api as {
  getOcrQueue: ReturnType<typeof vi.fn>
  triggerOcr: ReturnType<typeof vi.fn>
  triggerBatchOcr: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useOcrQueue', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns OCR queue data on success', async () => {
    const mockData = { items: [{ captureId: 'cap-1', status: 'Pending' }], total: 1 }
    mockApi.getOcrQueue.mockResolvedValue(mockData)
    const { result } = renderHook(() => useOcrQueue(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getOcrQueue.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useOcrQueue(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getOcrQueue.mockRejectedValue(new Error('server error'))
    const { result } = renderHook(() => useOcrQueue(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useTriggerOcr', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.triggerOcr with captureId', async () => {
    mockApi.triggerOcr.mockResolvedValue({ jobId: 'job-1' })
    mockApi.getOcrQueue.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useTriggerOcr(), { wrapper })
    result.current.mutate('cap-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.triggerOcr).toHaveBeenCalledWith('cap-1')
  })

  it('sets isError on failure', async () => {
    mockApi.triggerOcr.mockRejectedValue(new Error('cannot trigger'))
    const { result } = renderHook(() => useTriggerOcr(), { wrapper })
    result.current.mutate('cap-bad')
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useBatchOcr', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.triggerBatchOcr with examId', async () => {
    mockApi.triggerBatchOcr.mockResolvedValue({ queued: 5 })
    mockApi.getOcrQueue.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useBatchOcr(), { wrapper })
    result.current.mutate('exam-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.triggerBatchOcr).toHaveBeenCalledWith('exam-1')
  })

  it('sets isError on failure', async () => {
    mockApi.triggerBatchOcr.mockRejectedValue(new Error('batch failed'))
    const { result } = renderHook(() => useBatchOcr(), { wrapper })
    result.current.mutate('exam-bad')
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
