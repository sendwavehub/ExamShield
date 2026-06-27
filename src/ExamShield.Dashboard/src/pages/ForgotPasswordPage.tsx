import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'

export default function ForgotPasswordPage() {
  const [params] = useSearchParams()
  const token = params.get('token')

  return token ? <ResetPasswordForm token={token} /> : <ForgotPasswordForm />
}

function ForgotPasswordForm() {
  const [email, setEmail]       = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [loading, setLoading]   = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    try {
      await api.forgotPassword(email)
    } finally {
      setSubmitted(true)
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="w-full max-w-sm rounded-lg border border-border bg-card p-8 shadow-lg">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold text-foreground">Forgot Password</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Enter your email and we'll send a reset link.
          </p>
        </div>

        {submitted ? (
          <div className="space-y-4 text-center">
            <p className="text-sm text-muted-foreground">
              If that email exists, a reset link has been sent.
            </p>
            <Link to="/login" className="text-sm text-primary hover:underline">
              Back to Sign In
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} noValidate className="space-y-4">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-foreground">
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                autoFocus
                value={email}
                onChange={e => setEmail(e.target.value)}
                className="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                placeholder="you@example.com"
              />
            </div>

            <button
              type="submit"
              disabled={loading || !email.trim()}
              className="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {loading ? 'Sending…' : 'Send Reset Link'}
            </button>

            <p className="text-center text-sm text-muted-foreground">
              <Link to="/login" className="text-primary hover:underline">
                Back to Sign In
              </Link>
            </p>
          </form>
        )}
      </div>
    </div>
  )
}

function ResetPasswordForm({ token }: { token: string }) {
  const [password, setPassword]   = useState('')
  const [confirm, setConfirm]     = useState('')
  const [loading, setLoading]     = useState(false)
  const [done, setDone]           = useState(false)
  const [error, setError]         = useState('')

  const mismatch = password && confirm && password !== confirm

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (password !== confirm) return
    setLoading(true)
    setError('')
    try {
      await api.resetPassword(token, password)
      setDone(true)
    } catch {
      setError('This reset link is invalid or has already been used.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="w-full max-w-sm rounded-lg border border-border bg-card p-8 shadow-lg">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold text-foreground">Reset Password</h1>
          <p className="mt-1 text-sm text-muted-foreground">Choose a new password.</p>
        </div>

        {done ? (
          <div className="space-y-4 text-center">
            <p className="text-sm text-muted-foreground">
              Password updated. You can now sign in.
            </p>
            <Link to="/login" className="text-sm text-primary hover:underline">
              Sign In
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} noValidate className="space-y-4">
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-foreground">
                New Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="new-password"
                autoFocus
                value={password}
                onChange={e => setPassword(e.target.value)}
                className="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>

            <div>
              <label htmlFor="confirm" className="block text-sm font-medium text-foreground">
                Confirm Password
              </label>
              <input
                id="confirm"
                type="password"
                autoComplete="new-password"
                value={confirm}
                onChange={e => setConfirm(e.target.value)}
                className="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
              />
              {mismatch && (
                <p className="mt-1 text-xs text-red-500">Passwords do not match.</p>
              )}
            </div>

            {error && <p role="alert" className="text-sm text-red-500">{error}</p>}

            <button
              type="submit"
              disabled={loading || !password || !!mismatch}
              className="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
            >
              {loading ? 'Saving…' : 'Set New Password'}
            </button>

            <p className="text-center text-sm text-muted-foreground">
              <Link to="/login" className="text-primary hover:underline">
                Back to Sign In
              </Link>
            </p>
          </form>
        )}
      </div>
    </div>
  )
}
