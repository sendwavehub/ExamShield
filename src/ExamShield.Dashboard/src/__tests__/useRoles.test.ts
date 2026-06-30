import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: { getRoles: vi.fn() },
}))

import { api } from '../api/client'
import { useRoles } from '../hooks/useRoles'

const mockApi = api as { getRoles: ReturnType<typeof vi.fn> }

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useRoles', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns roles list on success', async () => {
    const mockData = [{ id: 'role-1', name: 'Administrator' }, { id: 'role-2', name: 'Invigilator' }]
    mockApi.getRoles.mockResolvedValue(mockData)
    const { result } = renderHook(() => useRoles(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getRoles.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useRoles(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getRoles.mockRejectedValue(new Error('forbidden'))
    const { result } = renderHook(() => useRoles(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })

  it('calls api.getRoles exactly once on mount', async () => {
    mockApi.getRoles.mockResolvedValue([])
    renderHook(() => useRoles(), { wrapper })
    await waitFor(() => expect(mockApi.getRoles).toHaveBeenCalledTimes(1))
  })
})
