import { useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useAuditLog } from '../hooks/useAuditLog'
import VerificationBadge from '../components/ui/VerificationBadge'
import type { AuditEntry } from '../api/client'

const PAGE_SIZE = 20

function formatDate(iso: string) {
  return new Date(iso).toLocaleString()
}

function truncate(str: string, n = 12) {
  return str.length > n ? `${str.slice(0, n)}…` : str
}

function AuditTable({ entries }: { entries: AuditEntry[] }) {
  return (
    <div className="overflow-hidden rounded-xl border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            {['Action', 'User', 'Time', 'Hash', 'Integrity'].map(h => (
              <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {entries.map(entry => (
            <tr key={entry.id} className="hover:bg-muted/30 transition-colors">
              <td className="px-4 py-3 font-medium text-foreground">{entry.action}</td>
              <td className="px-4 py-3 text-muted-foreground">{entry.userId}</td>
              <td className="px-4 py-3 text-muted-foreground">{formatDate(entry.occurredAt)}</td>
              <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                {truncate(entry.contentHash)}
              </td>
              <td className="px-4 py-3">
                <VerificationBadge status={entry.serverSignature ? 'valid' : 'pending'} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default function AuditLogPage() {
  const [page, setPage] = useState(1)
  const { data, isLoading, isError } = useAuditLog(page, PAGE_SIZE)
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-foreground">Audit Log</h1>
        {data && <span className="text-sm text-muted-foreground">{data.totalCount} entries</span>}
      </div>

      {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
      {isError  && <p className="text-sm text-red-500">Failed to load audit log.</p>}
      {data     && <AuditTable entries={data.entries} />}

      {data && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>Page {page} of {totalPages}</span>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded-md border border-border px-3 py-1 hover:bg-muted disabled:opacity-40"
              aria-label="Previous page"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="rounded-md border border-border px-3 py-1 hover:bg-muted disabled:opacity-40"
              aria-label="Next page"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
