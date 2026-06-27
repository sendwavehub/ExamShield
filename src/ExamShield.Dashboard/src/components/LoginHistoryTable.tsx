import type { LoginHistoryEntry } from '../api/client'

interface Props {
  entries: LoginHistoryEntry[]
}

export default function LoginHistoryTable({ entries }: Props) {
  return (
    <div>
      <table className="w-full text-sm text-left">
        <thead>
          <tr className="border-b border-border text-muted-foreground">
            <th className="pb-2 pr-4 font-medium">Event</th>
            <th className="pb-2 pr-4 font-medium">IP Address</th>
            <th className="pb-2 font-medium">Occurred</th>
          </tr>
        </thead>
        <tbody>
          {entries.map(entry => (
            <tr key={entry.id} className="border-b border-border/50">
              <td className="py-2 pr-4">
                <span
                  className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-medium ${
                    entry.eventType === 'LoginSuccess'
                      ? 'bg-green-500/10 text-green-400'
                      : 'bg-red-500/10 text-red-400'
                  }`}
                >
                  {entry.eventType}
                </span>
              </td>
              <td className="py-2 pr-4 text-foreground">{entry.ipAddress ?? '—'}</td>
              <td className="py-2 text-muted-foreground">
                {new Date(entry.occurredAt).toLocaleString()}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {entries.length === 0 && (
        <p className="py-8 text-center text-sm text-muted-foreground">No login history available.</p>
      )}
    </div>
  )
}
