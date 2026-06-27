import { useState } from 'react'
import { useDevices, useApproveDevice, useDisableDevice, useEnableDevice, useBlacklistDevice, useDeviceHeartbeat } from '../hooks/useDevices'
import StatusChip from '../components/ui/StatusChip'
import type { DeviceEntry } from '../api/client'

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString()
}

function healthStatus(lastSeenAt: string | null): { label: string; variant: 'success' | 'warning' | 'danger' | 'muted' } {
  if (!lastSeenAt) return { label: 'Never seen', variant: 'muted' }
  const ageMs = Date.now() - new Date(lastSeenAt).getTime()
  if (ageMs < 2 * 60_000)  return { label: 'Online',  variant: 'success' }
  if (ageMs < 10 * 60_000) return { label: 'Recent',  variant: 'warning' }
  return { label: 'Stale', variant: 'danger' }
}

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'muted'> = {
  Approved:    'success',
  Pending:     'warning',
  Disabled:    'danger',
  Blacklisted: 'danger',
}

function DeviceRow({ device }: { device: DeviceEntry }) {
  const approve   = useApproveDevice()
  const disable   = useDisableDevice()
  const enable    = useEnableDevice()
  const blacklist = useBlacklistDevice()
  const heartbeat = useDeviceHeartbeat()
  const busy      = approve.isPending || disable.isPending || enable.isPending || blacklist.isPending
  const [showBlacklistInput, setShowBlacklistInput] = useState(false)
  const [blacklistReason, setBlacklistReason]       = useState('')

  const health = healthStatus(device.lastSeenAt)

  return (
    <tr className="hover:bg-muted/30 transition-colors">
      <td className="px-4 py-3 font-medium text-foreground">{device.name}</td>
      <td className="px-4 py-3">
        <StatusChip
          label={device.status}
          variant={STATUS_VARIANT[device.status] ?? 'muted'}
        />
      </td>
      <td className="px-4 py-3">
        <StatusChip label={health.label} variant={health.variant} />
      </td>
      <td className="px-4 py-3 text-sm text-muted-foreground">
        {device.lastSeenAt ? new Date(device.lastSeenAt).toLocaleTimeString() : '—'}
      </td>
      <td className="px-4 py-3 text-sm text-muted-foreground">{formatDate(device.registeredAt)}</td>
      <td className="px-4 py-3">
        <div className="flex gap-2">
          <button
            onClick={() => heartbeat.mutate(device.deviceId)}
            disabled={!device.isActive || heartbeat.isPending}
            className="rounded-md border border-blue-500/40 px-3 py-1 text-xs text-blue-400 hover:bg-blue-500/10 disabled:opacity-40"
            title="Simulate heartbeat ping"
          >
            Ping
          </button>
          {device.status === 'Pending' && (
            <button
              onClick={() => approve.mutate(device.deviceId)}
              disabled={busy}
              className="rounded-md border border-green-500/40 px-3 py-1 text-xs text-green-500 hover:bg-green-500/10 disabled:opacity-40"
            >
              Approve
            </button>
          )}
          {device.status === 'Approved' && (
            <button
              onClick={() => disable.mutate(device.deviceId)}
              disabled={busy}
              className="rounded-md border border-red-500/40 px-3 py-1 text-xs text-red-500 hover:bg-red-500/10 disabled:opacity-40"
            >
              Disable
            </button>
          )}
          {device.status === 'Disabled' && (
            <button
              onClick={() => enable.mutate(device.deviceId)}
              disabled={busy}
              className="rounded-md border border-yellow-500/40 px-3 py-1 text-xs text-yellow-500 hover:bg-yellow-500/10 disabled:opacity-40"
            >
              Re-enable
            </button>
          )}
          {device.status !== 'Blacklisted' && !showBlacklistInput && (
            <button
              onClick={() => setShowBlacklistInput(true)}
              disabled={busy}
              className="rounded-md border border-rose-700/40 px-3 py-1 text-xs text-rose-600 hover:bg-rose-700/10 disabled:opacity-40"
            >
              Blacklist
            </button>
          )}
          {showBlacklistInput && (
            <div className="flex items-center gap-1">
              <input
                value={blacklistReason}
                onChange={e => setBlacklistReason(e.target.value)}
                placeholder="Reason…"
                className="rounded border px-2 py-1 text-xs bg-background w-28"
              />
              <button
                disabled={!blacklistReason.trim() || blacklist.isPending}
                onClick={() => blacklist.mutate({ id: device.deviceId, reason: blacklistReason }, {
                  onSuccess: () => { setShowBlacklistInput(false); setBlacklistReason('') },
                })}
                className="rounded px-2 py-1 text-xs bg-rose-700 text-white disabled:opacity-40"
              >
                Confirm
              </button>
              <button onClick={() => setShowBlacklistInput(false)}
                className="rounded px-2 py-1 text-xs border border-border text-muted-foreground">
                ✕
              </button>
            </div>
          )}
          {device.status === 'Blacklisted' && device.blacklistReason && (
            <span className="text-xs text-rose-600/70 italic" title={device.blacklistReason}>
              {device.blacklistReason.slice(0, 30)}{device.blacklistReason.length > 30 ? '…' : ''}
            </span>
          )}
        </div>
      </td>
    </tr>
  )
}

export default function DeviceManagementPage() {
  const { data, isLoading, isError } = useDevices()

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-foreground">Device Management</h1>
        {data && (
          <span className="text-sm text-muted-foreground">{data.devices.length} devices</span>
        )}
      </div>

      {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
      {isError   && <p className="text-sm text-red-500">Failed to load devices.</p>}

      {data && (
        <div className="overflow-hidden rounded-xl border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                {['Device Name', 'Status', 'Health', 'Last Seen', 'Registered', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.devices.map(device => (
                <DeviceRow key={device.deviceId} device={device} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
