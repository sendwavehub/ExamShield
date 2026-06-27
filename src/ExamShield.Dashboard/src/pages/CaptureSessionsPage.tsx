import { useCaptures, useVerifyCapture } from '../hooks/useCaptures'
import StatusChip from '../components/ui/StatusChip'

const STATUS_VARIANT: Record<string, 'success' | 'info' | 'warning' | 'danger' | 'muted'> = {
  Verified: 'success',
  Uploaded: 'info',
  Created:  'warning',
  Tampered: 'danger',
}

export default function CaptureSessionsPage() {
  const { data, isLoading } = useCaptures()
  const verify = useVerifyCapture()

  if (isLoading) return <p>Loading...</p>

  const captures = data?.captures ?? []

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-bold">Capture Sessions</h1>
        <span className="text-sm text-muted-foreground">{captures.length} captures</span>
      </div>

      {captures.length === 0 ? (
        <p className="text-muted-foreground">No captures found.</p>
      ) : (
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="border-b text-left">
              <th className="py-2 pr-4">Capture ID</th>
              <th className="py-2 pr-4">Student ID</th>
              <th className="py-2 pr-4">Exam ID</th>
              <th className="py-2 pr-4">Status</th>
              <th className="py-2 pr-4">Captured At</th>
              <th className="py-2" />
            </tr>
          </thead>
          <tbody>
            {captures.map(cap => (
              <tr key={cap.captureId} className="border-b hover:bg-muted/30">
                <td className="py-2 pr-4 font-mono text-xs">{cap.captureId}</td>
                <td className="py-2 pr-4 font-mono text-xs">{cap.studentId}</td>
                <td className="py-2 pr-4 font-mono text-xs">{cap.examId}</td>
                <td className="py-2 pr-4">
                  <StatusChip
                    variant={STATUS_VARIANT[cap.status] ?? 'muted'}
                    label={cap.status}
                  />
                </td>
                <td className="py-2 pr-4 text-muted-foreground">
                  {new Date(cap.capturedAt).toLocaleString()}
                </td>
                <td className="py-2">
                  {cap.storageKey && (
                    <button
                      onClick={() => verify.mutate(cap.captureId)}
                      disabled={verify.isPending}
                      className="px-3 py-1 text-xs rounded border border-primary text-primary hover:bg-primary/10 disabled:opacity-50"
                    >
                      Verify
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
