import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'

type SetupData = { secret: string; qrUri: string }

export default function MfaPage() {
  const qc = useQueryClient()
  const [setup, setSetup] = useState<SetupData | null>(null)
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: status, isLoading } = useQuery({
    queryKey: ['mfa-status'],
    queryFn: api.getMfaStatus,
  })

  const setupMut = useMutation({
    mutationFn: api.setupMfa,
    onSuccess: (data) => setSetup(data),
  })

  const verifyMut = useMutation({
    mutationFn: (c: string) => api.verifyMfa(c),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['mfa-status'] }); setSetup(null) },
    onError: () => setError('Invalid code. Please try again.'),
  })

  const disableMut = useMutation({
    mutationFn: api.disableMfa,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['mfa-status'] }),
  })

  if (isLoading) {
    return (
      <div className="p-8 text-center text-[#8B949E]">Loading...</div>
    )
  }

  return (
    <div className="p-8 max-w-xl">
      <h1 className="text-2xl font-bold text-white mb-2">Multi-Factor Authentication</h1>
      <p className="text-[#8B949E] mb-6">Protect your account with a time-based one-time password.</p>

      <div className="bg-[#161B22] rounded-xl border border-[#30363D] p-6 mb-6">
        <div className="flex items-center gap-3 mb-4">
          <span className={`text-sm font-semibold px-2 py-0.5 rounded ${
            status?.mfaEnabled
              ? 'bg-green-900/40 text-green-400'
              : 'bg-yellow-900/40 text-yellow-400'
          }`}>
            {status?.mfaEnabled ? 'Enabled' : 'Not enabled'}
          </span>
          <span className="text-[#8B949E] text-sm">
            {status?.mfaEnabled
              ? 'Your account is protected with TOTP.'
              : 'Your account does not have MFA enabled.'}
          </span>
        </div>

        {!status?.mfaEnabled && !setup && (
          <button
            onClick={() => setupMut.mutate()}
            disabled={setupMut.isPending}
            className="px-4 py-2 bg-[#00BFFF] text-black font-semibold rounded-lg disabled:opacity-50"
          >
            {setupMut.isPending ? 'Generating…' : 'Enable MFA'}
          </button>
        )}

        {status?.mfaEnabled && (
          <button
            onClick={() => disableMut.mutate()}
            disabled={disableMut.isPending}
            className="px-4 py-2 bg-red-700 text-white font-semibold rounded-lg disabled:opacity-50"
          >
            {disableMut.isPending ? 'Disabling…' : 'Disable MFA'}
          </button>
        )}
      </div>

      {setup && (
        <div className="bg-[#161B22] rounded-xl border border-[#30363D] p-6 space-y-4">
          <p className="text-white font-semibold">Scan this secret in your authenticator app</p>
          <p className="text-[#8B949E] text-xs break-all font-mono">{setup.secret}</p>
          <p className="text-[#8B949E] text-xs break-all">{setup.qrUri}</p>

          <div className="mt-4">
            <label className="block text-sm text-[#8B949E] mb-1">Enter the 6-digit code to confirm</label>
            <input
              type="text"
              maxLength={6}
              placeholder="6-digit code"
              value={code}
              onChange={(e) => { setCode(e.target.value); setError(null) }}
              className="w-full bg-[#0D1117] border border-[#30363D] rounded-lg px-3 py-2 text-white font-mono text-center tracking-widest text-lg"
            />
            {error && <p className="text-red-400 text-sm mt-1">{error}</p>}
            <button
              onClick={() => verifyMut.mutate(code)}
              disabled={code.length !== 6 || verifyMut.isPending}
              className="mt-3 w-full px-4 py-2 bg-[#00BFFF] text-black font-semibold rounded-lg disabled:opacity-50"
            >
              {verifyMut.isPending ? 'Verifying…' : 'Verify'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
