import { useState } from 'react'
import { api } from '../api/client'

export default function ChangePasswordPage() {
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(false)
    if (!current) { setError('Current password is required'); return }
    if (!next) { setError('New password is required'); return }
    setLoading(true)
    try {
      await api.changePassword(current, next)
      setSuccess(true)
      setCurrent('')
      setNext('')
    } catch {
      setError('Current password is incorrect')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-md">
      <h2 className="text-xl font-semibold text-foreground mb-6">Change Password</h2>
      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        <div>
          <label htmlFor="current-password" className="block text-sm font-medium text-foreground mb-1">
            Current Password
          </label>
          <input
            id="current-password"
            type="password"
            autoComplete="current-password"
            value={current}
            onChange={e => setCurrent(e.target.value)}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
          />
        </div>
        <div>
          <label htmlFor="new-password" className="block text-sm font-medium text-foreground mb-1">
            New Password
          </label>
          <input
            id="new-password"
            type="password"
            autoComplete="new-password"
            value={next}
            onChange={e => setNext(e.target.value)}
            className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
          />
        </div>
        {error && <p role="alert" className="text-sm text-red-500">{error}</p>}
        {success && <p role="status" className="text-sm text-green-500">Password changed successfully.</p>}
        <button
          type="submit"
          disabled={loading}
          className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
        >
          {loading ? 'Updating…' : 'Change Password'}
        </button>
      </form>
    </div>
  )
}
