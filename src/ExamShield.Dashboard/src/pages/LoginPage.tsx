import { useState } from 'react'

interface LoginPageProps {
  onLogin?: (email: string, password: string) => Promise<void>
  onMfaLogin?: (code: string) => Promise<void>
  requiresMfa?: boolean
}

export default function LoginPage({ onLogin, onMfaLogin, requiresMfa }: LoginPageProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [mfaCode, setMfaCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (requiresMfa) {
      if (!mfaCode) { setError('Authenticator code is required'); return }
      setLoading(true)
      try { await onMfaLogin?.(mfaCode) }
      catch { setError('Invalid authenticator code') }
      finally { setLoading(false) }
      return
    }

    if (!email) { setError('Email is required'); return }
    if (!password) { setError('Password is required'); return }

    setLoading(true)
    try { await onLogin?.(email, password) }
    catch { setError('Invalid credentials') }
    finally { setLoading(false) }
  }

  if (requiresMfa) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="w-full max-w-sm rounded-lg border border-border bg-card p-8 shadow-lg">
          <div className="mb-6 text-center">
            <h1 className="text-2xl font-bold text-foreground">ExamShield</h1>
            <p className="mt-1 text-sm text-muted-foreground">Enter your authenticator code</p>
          </div>
          <form onSubmit={handleSubmit} noValidate className="space-y-4">
            <div>
              <label htmlFor="mfa-code" className="block text-sm font-medium text-foreground">
                Authenticator Code
              </label>
              <input
                id="mfa-code"
                type="text"
                inputMode="numeric"
                maxLength={6}
                autoComplete="one-time-code"
                value={mfaCode}
                onChange={e => setMfaCode(e.target.value.replace(/\D/g, ''))}
                className="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-center text-lg tracking-widest text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                placeholder="000000"
                autoFocus
              />
            </div>
            {error && <p role="alert" className="text-sm text-red-500">{error}</p>}
            <button
              type="submit"
              disabled={loading}
              className="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {loading ? 'Verifying…' : 'Verify'}
            </button>
          </form>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="w-full max-w-sm rounded-lg border border-border bg-card p-8 shadow-lg">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold text-foreground">ExamShield</h1>
          <p className="mt-1 text-sm text-muted-foreground">Sign in to your account</p>
        </div>

        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-foreground">
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              className="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
              placeholder="admin@examshield.local"
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-foreground">
              Password
            </label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              className="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          {error && (
            <p role="alert" className="text-sm text-red-500">{error}</p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
          >
            {loading ? 'Signing in…' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  )
}
