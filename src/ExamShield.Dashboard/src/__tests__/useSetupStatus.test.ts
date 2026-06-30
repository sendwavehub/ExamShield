import { renderHook, waitFor, act } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'

vi.mock('../api/client', () => ({
  api: { getSetupStatus: vi.fn() },
}))

import { api } from '../api/client'
import { useSetupStatus } from '../hooks/useSetupStatus'

const mockApi = api as { getSetupStatus: ReturnType<typeof vi.fn> }

const mockStatus = { isConfigured: true, hasAdmin: true, dbConnected: true }

describe('useSetupStatus', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('starts in loading state', () => {
    mockApi.getSetupStatus.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useSetupStatus())
    expect(result.current.loading).toBe(true)
    expect(result.current.status).toBeNull()
  })

  it('resolves status after successful fetch', async () => {
    mockApi.getSetupStatus.mockResolvedValue(mockStatus)
    const { result } = renderHook(() => useSetupStatus())
    await waitFor(() => expect(result.current.loading).toBe(false))
    expect(result.current.status).toEqual(mockStatus)
    expect(result.current.error).toBeNull()
  })

  it('sets error and clears loading on rejection', async () => {
    mockApi.getSetupStatus.mockRejectedValue(new Error('Network error'))
    const { result } = renderHook(() => useSetupStatus())
    await waitFor(() => expect(result.current.loading).toBe(false))
    expect(result.current.error).toBe('Network error')
    expect(result.current.status).toBeNull()
  })

  it('uses fallback error message for non-Error rejections', async () => {
    mockApi.getSetupStatus.mockRejectedValue('oops')
    const { result } = renderHook(() => useSetupStatus())
    await waitFor(() => expect(result.current.loading).toBe(false))
    expect(result.current.error).toBe('Cannot reach API')
  })

  it('refresh() re-fetches and updates status', async () => {
    mockApi.getSetupStatus
      .mockResolvedValueOnce({ isConfigured: false })
      .mockResolvedValueOnce(mockStatus)

    const { result } = renderHook(() => useSetupStatus())
    await waitFor(() => expect(result.current.loading).toBe(false))
    expect(result.current.status).toEqual({ isConfigured: false })

    await act(async () => { await result.current.refresh() })
    expect(result.current.status).toEqual(mockStatus)
    expect(mockApi.getSetupStatus).toHaveBeenCalledTimes(2)
  })

  it('refresh() sets loading=true while in flight', async () => {
    let resolve!: (v: unknown) => void
    mockApi.getSetupStatus
      .mockResolvedValueOnce(mockStatus)
      .mockImplementationOnce(() => new Promise(r => { resolve = r }))

    const { result } = renderHook(() => useSetupStatus())
    await waitFor(() => expect(result.current.loading).toBe(false))

    act(() => { result.current.refresh() })
    expect(result.current.loading).toBe(true)
    await act(async () => { resolve(mockStatus) })
  })
})
