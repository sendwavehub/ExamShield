import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getUsers: vi.fn(),
    getUserById: vi.fn(),
    updateUserProfile: vi.fn(),
    updateUserRole: vi.fn(),
    deactivateUser: vi.fn(),
    activateUser: vi.fn(),
  },
}))

import { api } from '../api/client'
import {
  useUsers,
  useUserDetail,
  useUpdateUserProfile,
  useUpdateUserRole,
  useDeactivateUser,
  useActivateUser,
} from '../hooks/useUsers'

const mockApi = api as Record<string, ReturnType<typeof vi.fn>>

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockUsers = { items: [{ id: 'usr-1', displayName: 'Alice', role: 'Invigilator' }], total: 1 }

describe('useUsers', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns user list on success', async () => {
    mockApi.getUsers.mockResolvedValue(mockUsers)
    const { result } = renderHook(() => useUsers(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockUsers)
  })

  it('starts in loading state', () => {
    mockApi.getUsers.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useUsers(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getUsers.mockRejectedValue(new Error('forbidden'))
    const { result } = renderHook(() => useUsers(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })

  it('passes filters to api.getUsers', async () => {
    mockApi.getUsers.mockResolvedValue(mockUsers)
    renderHook(() => useUsers(2, 25, 'alice', 'Invigilator', true), { wrapper })
    await waitFor(() => expect(mockApi.getUsers).toHaveBeenCalledWith(2, 25, 'alice', 'Invigilator', true))
  })
})

describe('useUserDetail', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('is disabled when userId is null', () => {
    mockApi.getUserById.mockResolvedValue({})
    const { result } = renderHook(() => useUserDetail(null), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
    expect(mockApi.getUserById).not.toHaveBeenCalled()
  })

  it('fetches user detail when userId is provided', async () => {
    const user = { id: 'usr-1', displayName: 'Alice', role: 'Invigilator' }
    mockApi.getUserById.mockResolvedValue(user)
    const { result } = renderHook(() => useUserDetail('usr-1'), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(user)
    expect(mockApi.getUserById).toHaveBeenCalledWith('usr-1')
  })

  it('sets error state on failure', async () => {
    mockApi.getUserById.mockRejectedValue(new Error('not found'))
    const { result } = renderHook(() => useUserDetail('usr-bad'), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useUpdateUserProfile', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.updateUserProfile with userId and displayName', async () => {
    mockApi.updateUserProfile.mockResolvedValue({ id: 'usr-1', displayName: 'Alice Updated' })
    mockApi.getUsers.mockResolvedValue({ items: [], total: 0 })
    mockApi.getUserById.mockResolvedValue({})
    const { result } = renderHook(() => useUpdateUserProfile(), { wrapper })
    result.current.mutate({ userId: 'usr-1', displayName: 'Alice Updated' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.updateUserProfile).toHaveBeenCalledWith('usr-1', 'Alice Updated')
  })
})

describe('useUpdateUserRole', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.updateUserRole with userId and role', async () => {
    mockApi.updateUserRole.mockResolvedValue({ id: 'usr-1', role: 'Auditor' })
    mockApi.getUsers.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useUpdateUserRole(), { wrapper })
    result.current.mutate({ userId: 'usr-1', role: 'Auditor' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.updateUserRole).toHaveBeenCalledWith('usr-1', 'Auditor')
  })
})

describe('useDeactivateUser', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.deactivateUser with userId', async () => {
    mockApi.deactivateUser.mockResolvedValue({ active: false })
    mockApi.getUsers.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useDeactivateUser(), { wrapper })
    result.current.mutate('usr-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.deactivateUser).toHaveBeenCalledWith('usr-1')
  })
})

describe('useActivateUser', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.activateUser with userId', async () => {
    mockApi.activateUser.mockResolvedValue({ active: true })
    mockApi.getUsers.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useActivateUser(), { wrapper })
    result.current.mutate('usr-2')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.activateUser).toHaveBeenCalledWith('usr-2')
  })

  it('sets isError when activation fails', async () => {
    mockApi.activateUser.mockRejectedValue(new Error('not allowed'))
    const { result } = renderHook(() => useActivateUser(), { wrapper })
    result.current.mutate('usr-bad')
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
