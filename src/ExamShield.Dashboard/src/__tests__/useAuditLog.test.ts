import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: { getAuditLog: vi.fn() },
}))

import { api } from '../api/client'
import { useAuditLog } from '../hooks/useAuditLog'

const mockApi = api as { getAuditLog: ReturnType<typeof vi.fn> }

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useAuditLog', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns audit log data on success', async () => {
    const mockData = { items: [{ id: 'log-1', action: 'Capture' }], total: 1 }
    mockApi.getAuditLog.mockResolvedValue(mockData)
    const { result } = renderHook(() => useAuditLog(1), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getAuditLog.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useAuditLog(1), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getAuditLog.mockRejectedValue(new Error('forbidden'))
    const { result } = renderHook(() => useAuditLog(1), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })

  it('passes page, pageSize, captureId, action to api', async () => {
    mockApi.getAuditLog.mockResolvedValue({ items: [], total: 0 })
    renderHook(() => useAuditLog(2, 10, 'cap-1', 'Upload'), { wrapper })
    await waitFor(() => expect(mockApi.getAuditLog).toHaveBeenCalledWith({
      page: 2, pageSize: 10, captureId: 'cap-1', action: 'Upload',
    }))
  })

  it('uses default pageSize of 20 when not specified', async () => {
    mockApi.getAuditLog.mockResolvedValue({ items: [], total: 0 })
    renderHook(() => useAuditLog(1), { wrapper })
    await waitFor(() => expect(mockApi.getAuditLog).toHaveBeenCalledWith({
      page: 1, pageSize: 20, captureId: undefined, action: undefined,
    }))
  })
})
