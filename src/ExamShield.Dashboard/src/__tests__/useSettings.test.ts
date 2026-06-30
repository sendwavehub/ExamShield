import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getSettings: vi.fn(),
    updateSettings: vi.fn(),
    testAlert: vi.fn(),
  },
}))

import { api } from '../api/client'
import { useSettings, useUpdateSettings, useTestAlert } from '../hooks/useSettings'

const mockApi = api as {
  getSettings: ReturnType<typeof vi.fn>
  updateSettings: ReturnType<typeof vi.fn>
  testAlert: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockSettings = { alertEmail: 'admin@exam.com', alertSlack: true, alertWebhook: null }

describe('useSettings', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns settings data on success', async () => {
    mockApi.getSettings.mockResolvedValue(mockSettings)
    const { result } = renderHook(() => useSettings(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockSettings)
  })

  it('starts in loading state', () => {
    mockApi.getSettings.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useSettings(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getSettings.mockRejectedValue(new Error('server error'))
    const { result } = renderHook(() => useSettings(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useUpdateSettings', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.updateSettings with payload', async () => {
    const updatedSettings = { ...mockSettings, alertSlack: false }
    mockApi.updateSettings.mockResolvedValue(updatedSettings)
    const { result } = renderHook(() => useUpdateSettings(), { wrapper })
    result.current.mutate(updatedSettings as any)
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.updateSettings).toHaveBeenCalledWith(updatedSettings)
  })

  it('sets isError on failure', async () => {
    mockApi.updateSettings.mockRejectedValue(new Error('validation error'))
    const { result } = renderHook(() => useUpdateSettings(), { wrapper })
    result.current.mutate({} as any)
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useTestAlert', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.testAlert when mutate is invoked', async () => {
    mockApi.testAlert.mockResolvedValue({ sent: true })
    const { result } = renderHook(() => useTestAlert(), { wrapper })
    result.current.mutate()
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.testAlert).toHaveBeenCalledTimes(1)
  })

  it('sets isError when alert test fails', async () => {
    mockApi.testAlert.mockRejectedValue(new Error('alert service down'))
    const { result } = renderHook(() => useTestAlert(), { wrapper })
    result.current.mutate()
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
