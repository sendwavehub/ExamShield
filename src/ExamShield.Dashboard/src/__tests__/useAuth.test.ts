import { renderHook, act } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { useAuth } from '../hooks/useAuth'

vi.mock('../api/client', () => ({
  api: {
    login: vi.fn(),
    mfaLogin: vi.fn(),
    refreshToken: vi.fn(),
    logout: vi.fn(),
  },
}))

import { api } from '../api/client'

const mockApi = api as {
  login: ReturnType<typeof vi.fn>
  mfaLogin: ReturnType<typeof vi.fn>
  refreshToken: ReturnType<typeof vi.fn>
  logout: ReturnType<typeof vi.fn>
}

describe('useAuth', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('login stores token, role, and refresh token in localStorage', async () => {
    mockApi.login.mockResolvedValue({
      token: 'access-tok', refreshToken: 'ref-tok', role: 'Admin', requiresMfa: false,
    })
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.login('a@b.com', 'pw') })

    expect(localStorage.getItem('auth_token')).toBe('access-tok')
    expect(localStorage.getItem('auth_refresh_token')).toBe('ref-tok')
    expect(localStorage.getItem('auth_role')).toBe('Admin')
  })

  it('login sets requiresMfa when server returns requiresMfa=true', async () => {
    mockApi.login.mockResolvedValue({
      token: '', refreshToken: '', role: 'Admin', requiresMfa: true,
    })
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.login('a@b.com', 'pw') })

    expect(result.current.requiresMfa).toBe(true)
    expect(result.current.isAuthenticated).toBe(false)
    expect(localStorage.getItem('auth_token')).toBeNull()
  })

  it('completeMfaLogin stores tokens after successful TOTP verification', async () => {
    mockApi.login.mockResolvedValue({ token: '', refreshToken: '', role: 'Admin', requiresMfa: true })
    mockApi.mfaLogin.mockResolvedValue({
      token: 'full-tok', refreshToken: 'new-ref', role: 'Admin', requiresMfa: false,
    })
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.login('a@b.com', 'pw') })
    await act(async () => { await result.current.completeMfaLogin('123456') })

    expect(result.current.isAuthenticated).toBe(true)
    expect(result.current.requiresMfa).toBe(false)
    expect(localStorage.getItem('auth_token')).toBe('full-tok')
  })

  it('logout calls api.logout with stored refresh token', async () => {
    localStorage.setItem('auth_token', 'tok')
    localStorage.setItem('auth_refresh_token', 'old-ref')
    mockApi.logout.mockResolvedValue(undefined)
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.logout() })

    expect(mockApi.logout).toHaveBeenCalledWith('old-ref')
    expect(localStorage.getItem('auth_token')).toBeNull()
    expect(localStorage.getItem('auth_refresh_token')).toBeNull()
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('logout succeeds even when api.logout throws', async () => {
    localStorage.setItem('auth_token', 'tok')
    localStorage.setItem('auth_refresh_token', 'ref')
    mockApi.logout.mockRejectedValue(new Error('network'))
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.logout() })

    expect(result.current.isAuthenticated).toBe(false)
    expect(localStorage.getItem('auth_token')).toBeNull()
  })

  it('refresh stores new tokens from server', async () => {
    localStorage.setItem('auth_token', 'old-tok')
    localStorage.setItem('auth_refresh_token', 'old-ref')
    mockApi.refreshToken.mockResolvedValue({
      token: 'new-tok', refreshToken: 'new-ref', role: 'Admin', requiresMfa: false,
    })
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.refresh() })

    expect(localStorage.getItem('auth_token')).toBe('new-tok')
    expect(localStorage.getItem('auth_refresh_token')).toBe('new-ref')
  })
})
