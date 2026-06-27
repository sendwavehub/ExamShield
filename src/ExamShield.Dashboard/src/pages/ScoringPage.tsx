import { useScoringQueue, useScoreCapture } from '../hooks/useScoring'

export default function ScoringPage() {
  const { data, isLoading } = useScoringQueue()
  const score = useScoreCapture()

  if (isLoading) return <p>Loading...</p>

  const items = data?.items ?? []

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-bold">Scoring</h1>
        <span className="text-sm text-muted-foreground">{items.length} pending</span>
      </div>

      {items.length === 0 ? (
        <p className="text-muted-foreground">No captures awaiting scoring.</p>
      ) : (
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="border-b text-left">
              <th className="py-2 pr-4">Capture ID</th>
              <th className="py-2 pr-4">Exam ID</th>
              <th className="py-2 pr-4">Confidence</th>
              <th className="py-2 pr-4">Processed At</th>
              <th className="py-2" />
            </tr>
          </thead>
          <tbody>
            {items.map(item => (
              <tr key={item.captureId} className="border-b hover:bg-muted/30">
                <td className="py-2 pr-4 font-mono">{item.captureId}</td>
                <td className="py-2 pr-4 font-mono">{item.examId}</td>
                <td className="py-2 pr-4">
                  {Math.round(item.overallConfidence * 100)}%
                </td>
                <td className="py-2 pr-4 text-muted-foreground">
                  {new Date(item.completedAt).toLocaleString()}
                </td>
                <td className="py-2">
                  <button
                    onClick={() => score.mutate(item.captureId)}
                    disabled={score.isPending}
                    className="px-3 py-1 text-xs rounded bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                  >
                    Score
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
