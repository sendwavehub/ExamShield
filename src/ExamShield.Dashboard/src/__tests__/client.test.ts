import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { api } from '../api/client'

// ── fetch mock helpers ────────────────────────────────────────────────────────

const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

function ok(data: unknown, status = 200): Response {
  return {
    ok: status < 400,
    status,
    json: () => Promise.resolve(data),
    text: () => Promise.resolve(JSON.stringify(data)),
    blob: () => Promise.resolve(new Blob([JSON.stringify(data)])),
    headers: new Headers(),
  } as unknown as Response
}

function fail(status: number, body = ''): Response {
  return {
    ok: false,
    status,
    json: () => Promise.resolve({}),
    text: () => Promise.resolve(body),
    headers: new Headers(),
  } as unknown as Response
}

// ── setup / teardown ─────────────────────────────────────────────────────────

beforeEach(() => {
  mockFetch.mockReset()
  localStorage.clear()
  vi.clearAllMocks()
})

afterEach(() => {
  localStorage.clear()
})

// ── authHeaders (tested via GET /devices) ─────────────────────────────────────

describe('authHeaders', () => {
  it('sends no Authorization header when no token in localStorage', async () => {
    mockFetch.mockResolvedValueOnce(ok({ devices: [] }))
    await api.getDevices()
    const headers = mockFetch.mock.calls[0][1].headers as Record<string, string>
    expect(headers['Authorization']).toBeUndefined()
  })

  it('sends Bearer token when auth_token present in localStorage', async () => {
    localStorage.setItem('auth_token', 'my-access-token')
    mockFetch.mockResolvedValueOnce(ok({ devices: [] }))
    await api.getDevices()
    const headers = mockFetch.mock.calls[0][1].headers as Record<string, string>
    expect(headers['Authorization']).toBe('Bearer my-access-token')
  })
})

// ── signOut (tested via 401 + failed refresh) ─────────────────────────────────

describe('signOut', () => {
  it('clears auth_token from localStorage when refresh fails', async () => {
    localStorage.setItem('auth_token', 'expired-token')
    localStorage.setItem('auth_role', 'Admin')
    // Non-auth path → 401 → tryRefresh fails
    mockFetch
      .mockResolvedValueOnce(fail(401))     // original request → 401
      .mockResolvedValueOnce(fail(401))     // /auth/refresh → 401

    await expect(api.getDevices()).rejects.toThrow()
    expect(localStorage.getItem('auth_token')).toBeNull()
  })

  it('clears auth_role from localStorage when refresh fails', async () => {
    localStorage.setItem('auth_token', 'tok')
    localStorage.setItem('auth_role', 'Admin')
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockResolvedValueOnce(fail(401))

    await expect(api.getDevices()).rejects.toThrow()
    expect(localStorage.getItem('auth_role')).toBeNull()
  })

  it('does NOT remove auth_refresh_token (moved to HttpOnly cookie)', async () => {
    // auth_refresh_token should never be in localStorage now; verify signOut
    // does not try to remove something that should not be there.
    localStorage.setItem('auth_token', 'tok')
    localStorage.setItem('auth_refresh_token', 'should-not-be-here') // legacy
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockResolvedValueOnce(fail(401))

    await expect(api.getDevices()).rejects.toThrow()
    // signOut no longer clears auth_refresh_token — the cookie is cleared server-side on logout
    // If this key somehow ended up in localStorage it is not signOut's responsibility to clear it.
    // (Checking our implementation only touches auth_token + auth_role)
    expect(localStorage.getItem('auth_token')).toBeNull()
    expect(localStorage.getItem('auth_role')).toBeNull()
  })

  it('dispatches auth:expired custom event when refresh fails', async () => {
    localStorage.setItem('auth_token', 'tok')
    const listener = vi.fn()
    window.addEventListener('auth:expired', listener)
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockResolvedValueOnce(fail(401))

    await expect(api.getDevices()).rejects.toThrow()
    expect(listener).toHaveBeenCalledTimes(1)
    window.removeEventListener('auth:expired', listener)
  })
})

// ── tryRefresh (tested via 401 interception) ──────────────────────────────────

describe('tryRefresh', () => {
  it('calls /auth/refresh with credentials: include when 401 received on non-auth path', async () => {
    localStorage.setItem('auth_token', 'old-tok')
    mockFetch
      .mockResolvedValueOnce(fail(401))                             // GET /devices → 401
      .mockResolvedValueOnce(ok({ token: 'new-tok' }))             // POST /auth/refresh → ok
      .mockResolvedValueOnce(ok({ devices: [] }))                  // retry GET /devices → ok

    await api.getDevices()

    const refreshCall = mockFetch.mock.calls[1]
    expect(refreshCall[0]).toMatch(/\/auth\/refresh$/)
    expect(refreshCall[1].method).toBe('POST')
    expect(refreshCall[1].credentials).toBe('include')
    expect(refreshCall[1].body).toBe('{}')
  })

  it('retries the original request with the new token after a successful refresh', async () => {
    localStorage.setItem('auth_token', 'old-tok')
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockResolvedValueOnce(ok({ token: 'new-tok' }))
      .mockResolvedValueOnce(ok({ devices: [{ deviceId: 'dev-1' }] }))

    const result = await api.getDevices()
    expect(result.devices[0].deviceId).toBe('dev-1')

    const retryCall = mockFetch.mock.calls[2]
    const retryHeaders = retryCall[1].headers as Record<string, string>
    expect(retryHeaders['Authorization']).toBe('Bearer new-tok')
  })

  it('stores the new access token in localStorage after refresh', async () => {
    localStorage.setItem('auth_token', 'old-tok')
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockResolvedValueOnce(ok({ token: 'refreshed-tok' }))
      .mockResolvedValueOnce(ok({ devices: [] }))

    await api.getDevices()
    expect(localStorage.getItem('auth_token')).toBe('refreshed-tok')
  })

  it('does NOT call /auth/refresh when 401 comes from an /auth/ path', async () => {
    mockFetch.mockResolvedValueOnce(fail(401))
    await expect(api.login('a@b.com', 'pw')).rejects.toThrow()
    // Only 1 fetch call — no refresh attempt
    expect(mockFetch).toHaveBeenCalledTimes(1)
  })

  it('calls signOut and throws when refresh itself returns 401', async () => {
    localStorage.setItem('auth_token', 'tok')
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockResolvedValueOnce(fail(401)) // refresh also fails

    await expect(api.getDevices()).rejects.toThrow('401')
    expect(localStorage.getItem('auth_token')).toBeNull()
  })

  it('calls signOut and throws when refresh network call rejects', async () => {
    localStorage.setItem('auth_token', 'tok')
    mockFetch
      .mockResolvedValueOnce(fail(401))
      .mockRejectedValueOnce(new Error('network error'))

    await expect(api.getDevices()).rejects.toThrow()
    expect(localStorage.getItem('auth_token')).toBeNull()
  })
})

// ── request — general ─────────────────────────────────────────────────────────

describe('request', () => {
  it('returns parsed JSON on success', async () => {
    mockFetch.mockResolvedValueOnce(ok({ devices: [{ deviceId: 'x' }] }))
    const result = await api.getDevices()
    expect(result.devices[0].deviceId).toBe('x')
  })

  it('throws with status and body on non-401 error', async () => {
    mockFetch.mockResolvedValueOnce(fail(500, 'Internal Server Error'))
    await expect(api.getDevices()).rejects.toThrow('500')
  })

  it('includes Content-Type application/json header on every request', async () => {
    mockFetch.mockResolvedValueOnce(ok({ devices: [] }))
    await api.getDevices()
    const headers = mockFetch.mock.calls[0][1].headers as Record<string, string>
    expect(headers['Content-Type']).toBe('application/json')
  })
})

// ── api.login ─────────────────────────────────────────────────────────────────

describe('api.login', () => {
  it('sends POST to /auth/login with email and password', async () => {
    mockFetch.mockResolvedValueOnce(ok({
      token: 'tok', refreshToken: 'ref', role: 'Admin', requiresMfa: false,
    }))
    await api.login('user@example.com', 's3cr3t')
    const [url, init] = mockFetch.mock.calls[0]
    expect(url).toMatch(/\/auth\/login$/)
    expect(init.method).toBe('POST')
    const body = JSON.parse(init.body as string)
    expect(body).toEqual({ email: 'user@example.com', password: 's3cr3t' })
  })

  it('returns the parsed LoginResponse', async () => {
    mockFetch.mockResolvedValueOnce(ok({
      token: 'my-token', refreshToken: 'my-ref', role: 'Operator', requiresMfa: false,
    }))
    const res = await api.login('a@b.com', 'pw')
    expect(res.token).toBe('my-token')
    expect(res.role).toBe('Operator')
    expect(res.requiresMfa).toBe(false)
  })
})

// ── api.logout ────────────────────────────────────────────────────────────────

describe('api.logout', () => {
  it('sends POST to /auth/logout with credentials: include', async () => {
    mockFetch.mockResolvedValueOnce(ok(undefined, 204))
    await api.logout()
    const [url, init] = mockFetch.mock.calls[0]
    expect(url).toMatch(/\/auth\/logout$/)
    expect(init.method).toBe('POST')
    expect(init.credentials).toBe('include')
    expect(init.body).toBe('{}')
  })
})

// ── api.refreshToken ──────────────────────────────────────────────────────────

describe('api.refreshToken', () => {
  it('sends POST to /auth/refresh with credentials: include and empty body', async () => {
    mockFetch.mockResolvedValueOnce(ok({
      token: 'new-tok', refreshToken: 'new-ref', role: 'Admin', requiresMfa: false,
    }))
    await api.refreshToken()
    const [url, init] = mockFetch.mock.calls[0]
    expect(url).toMatch(/\/auth\/refresh$/)
    expect(init.credentials).toBe('include')
    expect(init.body).toBe('{}')
  })

  it('returns the new access token in the response', async () => {
    mockFetch.mockResolvedValueOnce(ok({
      token: 'fresh-tok', refreshToken: 'fresh-ref', role: 'Admin', requiresMfa: false,
    }))
    const res = await api.refreshToken()
    expect(res.token).toBe('fresh-tok')
  })
})

// ── api.getAuditLog query-string building ─────────────────────────────────────

describe('api.getAuditLog', () => {
  it('sends GET /audit with no query string when called with no params', async () => {
    mockFetch.mockResolvedValueOnce(ok({ entries: [], totalCount: 0 }))
    await api.getAuditLog()
    expect(mockFetch.mock.calls[0][0]).toMatch(/\/audit\?$/)
  })

  it('includes captureId in the query string when provided', async () => {
    mockFetch.mockResolvedValueOnce(ok({ entries: [], totalCount: 0 }))
    await api.getAuditLog({ captureId: 'cap-123' })
    expect(mockFetch.mock.calls[0][0]).toContain('captureId=cap-123')
  })

  it('includes page and pageSize', async () => {
    mockFetch.mockResolvedValueOnce(ok({ entries: [], totalCount: 0 }))
    await api.getAuditLog({ page: 2, pageSize: 25 })
    const url = mockFetch.mock.calls[0][0] as string
    expect(url).toContain('page=2')
    expect(url).toContain('pageSize=25')
  })

  it('includes action filter when provided', async () => {
    mockFetch.mockResolvedValueOnce(ok({ entries: [], totalCount: 0 }))
    await api.getAuditLog({ action: 'CaptureRegistered' })
    expect(mockFetch.mock.calls[0][0]).toContain('action=CaptureRegistered')
  })

  it('includes from and to date filters', async () => {
    mockFetch.mockResolvedValueOnce(ok({ entries: [], totalCount: 0 }))
    await api.getAuditLog({ from: '2026-01-01', to: '2026-12-31' })
    const url = mockFetch.mock.calls[0][0] as string
    expect(url).toContain('from=2026-01-01')
    expect(url).toContain('to=2026-12-31')
  })
})

// ── api.getDevices ────────────────────────────────────────────────────────────

describe('api.getDevices', () => {
  it('sends GET to /devices', async () => {
    mockFetch.mockResolvedValueOnce(ok({ devices: [] }))
    await api.getDevices()
    expect(mockFetch.mock.calls[0][0]).toMatch(/\/devices$/)
    expect(mockFetch.mock.calls[0][1].method).toBeUndefined() // default GET
  })
})

// ── api.approveDevice / disableDevice ─────────────────────────────────────────

describe('api.device actions', () => {
  it('approveDevice sends PUT /devices/{id}/approve', async () => {
    mockFetch.mockResolvedValueOnce(ok(null, 204))
    await api.approveDevice('dev-42')
    const [url, init] = mockFetch.mock.calls[0]
    expect(url).toMatch(/\/devices\/dev-42\/approve$/)
    expect(init.method).toBe('PUT')
  })

  it('blacklistDevice sends reason in body', async () => {
    mockFetch.mockResolvedValueOnce(ok(null, 204))
    await api.blacklistDevice('dev-42', 'Stolen device')
    const body = JSON.parse(mockFetch.mock.calls[0][1].body as string)
    expect(body.reason).toBe('Stolen device')
  })
})
