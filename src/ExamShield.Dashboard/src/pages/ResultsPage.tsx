import { useResults, useStatistics } from '../hooks/useResults'

function percentageColor(pct: number): string {
  if (pct >= 80) return 'text-green-500'
  if (pct >= 60) return 'text-yellow-500'
  return 'text-red-500'
}

export default function ResultsPage() {
  const results = useResults()
  const stats = useStatistics()

  if (results.isLoading || stats.isLoading) return <p>Loading...</p>

  const items = results.data?.results ?? []
  const s = stats.data

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold">Results</h1>

      {s && (
        <div className="grid grid-cols-4 gap-4">
          <StatCard label="Total Scored" value={s.totalPapersScored} />
          <StatCard label="Average" value={`${s.averagePercentage.toFixed(1)}%`} />
          <StatCard label="Highest" value={`${s.highestScore}%`} />
          <StatCard label="Lowest" value={`${s.lowestScore}%`} />
        </div>
      )}

      {items.length === 0 ? (
        <p className="text-muted-foreground">No results published yet.</p>
      ) : (
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="border-b text-left">
              <th className="py-2 pr-4">Student ID</th>
              <th className="py-2 pr-4">Exam ID</th>
              <th className="py-2 pr-4">Score</th>
              <th className="py-2 pr-4">Percentage</th>
              <th className="py-2 pr-4">Scored At</th>
            </tr>
          </thead>
          <tbody>
            {items.map(item => (
              <tr key={item.scoreId} className="border-b hover:bg-muted/30">
                <td className="py-2 pr-4 font-mono">{item.studentId}</td>
                <td className="py-2 pr-4 font-mono">{item.examId}</td>
                <td className="py-2 pr-4">{item.correctAnswers} / {item.totalQuestions}</td>
                <td className={`py-2 pr-4 font-semibold ${percentageColor(item.percentage)}`}>
                  {item.percentage.toFixed(1)}%
                </td>
                <td className="py-2 pr-4 text-muted-foreground">
                  {new Date(item.scoredAt).toLocaleString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

function StatCard({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-lg border p-4 space-y-1">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-2xl font-bold">{value}</p>
    </div>
  )
}
