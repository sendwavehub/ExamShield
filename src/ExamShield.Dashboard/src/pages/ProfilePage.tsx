import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, type ProfileResponse, type SessionItem } from '../api/client'

export default function ProfilePage() {
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [sessions, setSessions] = useState<SessionItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([api.getProfile(), api.getSessions()])
      .then(([p, s]) => { setProfile(p); setSessions(s.sessions) })
      .finally(() => setLoading(false))
  }, [])

  async function handleRevoke(sessionId: string) {
    await api.revokeSession(sessionId)
    const updated = await api.getSessions()
    setSessions(updated.sessions)
  }

  if (loading) return <p className="text-muted-foreground">Loading…</p>

  return (
    <div className="space-y-8 max-w-2xl">
      <section>
        <h2 className="text-xl font-semibold text-foreground mb-4">My Profile</h2>
        <div className="rounded-lg border border-border bg-card p-6 space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Email</span>
            <span className="text-sm font-medium text-foreground">{profile?.email}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Role</span>
            <span className="text-sm font-medium text-foreground">{profile?.role}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Multi-Factor Authentication</span>
            {profile?.mfaEnabled
              ? <span className="rounded bg-green-500/10 px-2 py-0.5 text-xs text-green-400">MFA Enabled</span>
              : <span className="rounded bg-yellow-500/10 px-2 py-0.5 text-xs text-yellow-400">MFA Disabled</span>
            }
          </div>
        </div>
        <div className="mt-4 flex gap-3">
          <Link
            to="/settings/password"
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90"
          >
            Change Password
          </Link>
          <Link
            to="/settings/mfa"
            className="rounded-md border border-border px-4 py-2 text-sm font-medium text-foreground hover:bg-muted"
          >
            Manage MFA
          </Link>
        </div>
      </section>

      <section>
        <h3 className="text-lg font-semibold text-foreground mb-4">Active Sessions</h3>
        {sessions.length === 0 ? (
          <p className="text-sm text-muted-foreground">No active sessions.</p>
        ) : (
          <div className="rounded-lg border border-border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30">
                <tr>
                  <th className="text-left px-4 py-2 text-muted-foreground font-medium">Session ID</th>
                  <th className="text-left px-4 py-2 text-muted-foreground font-medium">Created</th>
                  <th className="text-left px-4 py-2 text-muted-foreground font-medium">Expires</th>
                  <th className="px-4 py-2" />
                </tr>
              </thead>
              <tbody>
                {sessions.map(s => (
                  <tr key={s.id} className="border-t border-border">
                    <td className="px-4 py-2 font-mono text-xs text-foreground">{s.id}</td>
                    <td className="px-4 py-2 text-muted-foreground">
                      {new Date(s.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-2 text-muted-foreground">
                      {new Date(s.expiresAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-2 text-right">
                      <button
                        onClick={() => handleRevoke(s.id)}
                        className="rounded border border-red-500/40 px-2 py-0.5 text-xs text-red-400 hover:bg-red-500/10"
                      >
                        Revoke
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
