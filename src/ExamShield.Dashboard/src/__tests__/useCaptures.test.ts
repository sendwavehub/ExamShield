import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getCaptures: vi.fn(),
    verifyCapture: vi.fn(),
    getChainOfCustody: vi.fn(),
    flagCaptureAsTampered: vi.fn(),
  },
}))

import { api } from '../api/client'
import { useCaptures, useVerifyCapture, useChainOfCustody, useFlagCaptureAsTampered } from '../hooks/useCaptures'

const mockApi = api as {
  getCaptures: ReturnType<typeof vi.fn>
  verifyCapture: ReturnType<typeof vi.fn>
  getChainOfCustody: ReturnType<typeof vi.fn>
  flagCaptureAsTampered: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useCaptures', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns data on successful fetch', async () => {
    const mockData = { items: [{ captureId: 'cap-1' }], total: 1 }
    mockApi.getCaptures.mockResolvedValue(mockData)
    const { result } = renderHook(() => useCaptures(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getCaptures.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useCaptures(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getCaptures.mockRejectedValue(new Error('fetch failed'))
    const { result } = renderHook(() => useCaptures(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })

  it('passes filters to api.getCaptures', async () => {
    mockApi.getCaptures.mockResolvedValue({ items: [], total: 0 })
    renderHook(() => useCaptures(2, 10, 'exam-1', 'Verified', 'dev-1', 'stu-1'), { wrapper })
    await waitFor(() => expect(mockApi.getCaptures).toHaveBeenCalledWith(2, 10, 'exam-1', 'Verified', 'dev-1', 'stu-1'))
  })
})

describe('useChainOfCustody', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('is disabled when captureId is null', () => {
    mockApi.getChainOfCustody.mockResolvedValue([])
    const { result } = renderHook(() => useChainOfCustody(null), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
    expect(mockApi.getChainOfCustody).not.toHaveBeenCalled()
  })

  it('fetches data when captureId is provided', async () => {
    const mockData = [{ event: 'Captured', at: '2026-01-01' }]
    mockApi.getChainOfCustody.mockResolvedValue(mockData)
    const { result } = renderHook(() => useChainOfCustody('cap-123'), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
    expect(mockApi.getChainOfCustody).toHaveBeenCalledWith('cap-123')
  })

  it('sets error state on failure', async () => {
    mockApi.getChainOfCustody.mockRejectedValue(new Error('not found'))
    const { result } = renderHook(() => useChainOfCustody('cap-bad'), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useVerifyCapture', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.verifyCapture with captureId on mutate', async () => {
    mockApi.verifyCapture.mockResolvedValue({ verified: true })
    const { result } = renderHook(() => useVerifyCapture(), { wrapper })
    result.current.mutate('cap-999')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.verifyCapture).toHaveBeenCalledWith('cap-999')
  })

  it('sets isError on mutation failure', async () => {
    mockApi.verifyCapture.mockRejectedValue(new Error('verification failed'))
    const { result } = renderHook(() => useVerifyCapture(), { wrapper })
    result.current.mutate('cap-bad')
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useFlagCaptureAsTampered', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.flagCaptureAsTampered with captureId and reason', async () => {
    mockApi.flagCaptureAsTampered.mockResolvedValue({ flagged: true })
    const { result } = renderHook(() => useFlagCaptureAsTampered(), { wrapper })
    result.current.mutate({ captureId: 'cap-1', reason: 'suspicious' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.flagCaptureAsTampered).toHaveBeenCalledWith('cap-1', 'suspicious')
  })
})
