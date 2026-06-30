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

  it('login stores token and role in localStorage; refresh token stays in HttpOnly cookie', async () => {
    mockApi.login.mockResolvedValue({
      token: 'access-tok', refreshToken: 'ref-tok', role: 'Admin', requiresMfa: false,
    })
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.login('a@b.com', 'pw') })

    expect(localStorage.getItem('auth_token')).toBe('access-tok')
    expect(localStorage.getItem('auth_role')).toBe('Admin')
    // Refresh token must NOT be in localStorage — it lives in the server-set HttpOnly cookie.
    expect(localStorage.getItem('auth_refresh_token')).toBeNull()
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

  it('logout calls api.logout (no token arg — uses HttpOnly cookie) and clears localStorage', async () => {
    localStorage.setItem('auth_token', 'tok')
    mockApi.logout.mockResolvedValue(undefined)
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.logout() })

    expect(mockApi.logout).toHaveBeenCalledWith()
    expect(localStorage.getItem('auth_token')).toBeNull()
    expect(localStorage.getItem('auth_refresh_token')).toBeNull()
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('logout succeeds even when api.logout throws', async () => {
    localStorage.setItem('auth_token', 'tok')
    mockApi.logout.mockRejectedValue(new Error('network'))
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.logout() })

    expect(result.current.isAuthenticated).toBe(false)
    expect(localStorage.getItem('auth_token')).toBeNull()
  })

  it('refresh calls api.refreshToken (no arg — cookie sent automatically) and stores new access token', async () => {
    localStorage.setItem('auth_token', 'old-tok')
    mockApi.refreshToken.mockResolvedValue({
      token: 'new-tok', refreshToken: 'new-ref', role: 'Admin', requiresMfa: false,
    })
    const { result } = renderHook(() => useAuth())

    await act(async () => { await result.current.refresh() })

    expect(mockApi.refreshToken).toHaveBeenCalledWith()
    expect(localStorage.getItem('auth_token')).toBe('new-tok')
    // Refresh token not stored in localStorage — lives in HttpOnly cookie.
    expect(localStorage.getItem('auth_refresh_token')).toBeNull()
  })
})
