import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getDevices: vi.fn(),
    approveDevice: vi.fn(),
    disableDevice: vi.fn(),
    enableDevice: vi.fn(),
    blacklistDevice: vi.fn(),
    deviceHeartbeat: vi.fn(),
  },
}))

import { api } from '../api/client'
import {
  useDevices,
  useApproveDevice,
  useDisableDevice,
  useEnableDevice,
  useBlacklistDevice,
  useDeviceHeartbeat,
} from '../hooks/useDevices'

const mockApi = api as {
  getDevices: ReturnType<typeof vi.fn>
  approveDevice: ReturnType<typeof vi.fn>
  disableDevice: ReturnType<typeof vi.fn>
  enableDevice: ReturnType<typeof vi.fn>
  blacklistDevice: ReturnType<typeof vi.fn>
  deviceHeartbeat: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockDevices = [{ id: 'dev-1', name: 'iPad Pro', status: 'Active' }]

describe('useDevices', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns device list on success', async () => {
    mockApi.getDevices.mockResolvedValue(mockDevices)
    const { result } = renderHook(() => useDevices(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockDevices)
  })

  it('starts in loading state', () => {
    mockApi.getDevices.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useDevices(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error on failure', async () => {
    mockApi.getDevices.mockRejectedValue(new Error('not found'))
    const { result } = renderHook(() => useDevices(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useApproveDevice', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.approveDevice with device id', async () => {
    mockApi.approveDevice.mockResolvedValue({ status: 'Approved' })
    mockApi.getDevices.mockResolvedValue([])
    const { result } = renderHook(() => useApproveDevice(), { wrapper })
    result.current.mutate('dev-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.approveDevice).toHaveBeenCalledWith('dev-1')
  })
})

describe('useDisableDevice', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.disableDevice with device id', async () => {
    mockApi.disableDevice.mockResolvedValue({ status: 'Disabled' })
    mockApi.getDevices.mockResolvedValue([])
    const { result } = renderHook(() => useDisableDevice(), { wrapper })
    result.current.mutate('dev-2')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.disableDevice).toHaveBeenCalledWith('dev-2')
  })
})

describe('useEnableDevice', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.enableDevice with device id', async () => {
    mockApi.enableDevice.mockResolvedValue({ status: 'Active' })
    mockApi.getDevices.mockResolvedValue([])
    const { result } = renderHook(() => useEnableDevice(), { wrapper })
    result.current.mutate('dev-3')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.enableDevice).toHaveBeenCalledWith('dev-3')
  })
})

describe('useBlacklistDevice', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.blacklistDevice with id and reason', async () => {
    mockApi.blacklistDevice.mockResolvedValue({ status: 'Blacklisted' })
    mockApi.getDevices.mockResolvedValue([])
    const { result } = renderHook(() => useBlacklistDevice(), { wrapper })
    result.current.mutate({ id: 'dev-4', reason: 'stolen' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.blacklistDevice).toHaveBeenCalledWith('dev-4', 'stolen')
  })
})

describe('useDeviceHeartbeat', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.deviceHeartbeat with device id', async () => {
    mockApi.deviceHeartbeat.mockResolvedValue({ ok: true })
    mockApi.getDevices.mockResolvedValue([])
    const { result } = renderHook(() => useDeviceHeartbeat(), { wrapper })
    result.current.mutate('dev-5')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.deviceHeartbeat).toHaveBeenCalledWith('dev-5')
  })
})
