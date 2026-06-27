import { useDevices, useDisableDevice, useEnableDevice } from '../hooks/useDevices'
import StatusChip from '../components/ui/StatusChip'
import type { DeviceEntry } from '../api/client'

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString()
}

function DeviceRow({ device }: { device: DeviceEntry }) {
  const disable = useDisableDevice()
  const enable  = useEnableDevice()
  const busy    = disable.isPending || enable.isPending

  return (
    <tr className="hover:bg-muted/30 transition-colors">
      <td className="px-4 py-3 font-medium text-foreground">{device.name}</td>
      <td className="px-4 py-3">
        <StatusChip
          label={device.isActive ? 'Active' : 'Disabled'}
          variant={device.isActive ? 'success' : 'danger'}
        />
      </td>
      <td className="px-4 py-3 text-sm text-muted-foreground">{formatDate(device.registeredAt)}</td>
      <td className="px-4 py-3">
        {device.isActive ? (
          <button
            onClick={() => disable.mutate(device.deviceId)}
            disabled={busy}
            className="rounded-md border border-red-500/40 px-3 py-1 text-xs text-red-500 hover:bg-red-500/10 disabled:opacity-40"
          >
            Disable
          </button>
        ) : (
          <button
            onClick={() => enable.mutate(device.deviceId)}
            disabled={busy}
            className="rounded-md border border-green-500/40 px-3 py-1 text-xs text-green-500 hover:bg-green-500/10 disabled:opacity-40"
          >
            Enable
          </button>
        )}
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
                {['Device Name', 'Status', 'Registered', 'Actions'].map(h => (
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
