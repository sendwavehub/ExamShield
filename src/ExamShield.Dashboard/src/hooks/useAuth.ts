import { useState, useCallback } from 'react'
import { api } from '../api/client'

function parseJwt(token: string): Record<string, unknown> {
  try {
    return JSON.parse(atob(token.split('.')[1]))
  } catch {
    return {}
  }
}

export type AuthStep = 'idle' | 'mfa_required'

export interface AuthState {
  token: string | null
  role: string | null
  step: AuthStep
  pendingCredentials: { email: string; password: string } | null
}

export function useAuth() {
  const [auth, setAuth] = useState<AuthState>(() => ({
    token: localStorage.getItem('auth_token'),
    role: localStorage.getItem('auth_role'),
    step: 'idle' as AuthStep,
    pendingCredentials: null,
  }))

  const login = useCallback(async (email: string, password: string) => {
    const res = await api.login(email, password)
    if (res.requiresMfa) {
      setAuth(prev => ({ ...prev, step: 'mfa_required', pendingCredentials: { email, password } }))
      return
    }
    localStorage.setItem('auth_token', res.token)
    localStorage.setItem('auth_role', res.role)
    // Refresh token is stored in an HttpOnly cookie set by the server — not in localStorage.
    setAuth({ token: res.token, role: res.role, step: 'idle', pendingCredentials: null })
  }, [])

  const completeMfaLogin = useCallback(async (code: string) => {
    const creds = auth.pendingCredentials
    if (!creds) throw new Error('No pending MFA login')
    const res = await api.mfaLogin(creds.email, creds.password, code)
    localStorage.setItem('auth_token', res.token)
    localStorage.setItem('auth_role', res.role)
    setAuth({ token: res.token, role: res.role, step: 'idle', pendingCredentials: null })
  }, [auth.pendingCredentials])

  const refresh = useCallback(async () => {
    // Cookie is sent automatically via credentials: 'include' in the API client.
    const res = await api.refreshToken()
    localStorage.setItem('auth_token', res.token)
    setAuth(prev => ({ ...prev, token: res.token }))
  }, [])

  const logout = useCallback(async () => {
    // Server reads the HttpOnly cookie and revokes the refresh token.
    try { await api.logout() } catch { /* revoke best-effort */ }
    localStorage.removeItem('auth_token')
    localStorage.removeItem('auth_role')
    setAuth({ token: null, role: null, step: 'idle', pendingCredentials: null })
  }, [])

  const decoded = auth.token ? parseJwt(auth.token) : {}
  return {
    auth,
    login,
    completeMfaLogin,
    refresh,
    logout,
    isAuthenticated: !!auth.token,
    requiresMfa: auth.step === 'mfa_required',
    email: (decoded.email as string | undefined) ?? null,
    expiresAt: decoded.exp ? new Date((decoded.exp as number) * 1000) : null,
    hasMfa: decoded.amr === 'mfa',
  }
}
