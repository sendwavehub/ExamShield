import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'

function pctColor(p: number) {
  if (p >= 80) return 'text-green-500'
  if (p >= 60) return 'text-yellow-500'
  return 'text-red-500'
}

export default function StudentPortalPage() {
  const [inputId, setInputId] = useState('')
  const [studentId, setStudentId] = useState<string | null>(null)

  const { data, isFetching } = useQuery({
    queryKey: ['student-results', studentId],
    queryFn: () => api.getStudentResults(studentId!),
    enabled: studentId !== null,
  })

  const handleLookUp = () => {
    const trimmed = inputId.trim()
    if (trimmed) setStudentId(trimmed)
  }

  const handlePrint = () => window.print()

  return (
    <div className="p-6 space-y-6 max-w-3xl">
      <h1 className="text-2xl font-bold">Student Portal</h1>

      {/* Search */}
      <div className="flex gap-2">
        <input
          type="text"
          placeholder="Enter Student ID (UUID)"
          value={inputId}
          onChange={e => setInputId(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleLookUp()}
          className="flex-1 rounded border border-border bg-background px-3 py-2 text-sm text-foreground"
        />
        <button
          onClick={handleLookUp}
          disabled={isFetching}
          className="px-4 py-2 text-sm rounded bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
        >
          {isFetching ? 'Looking up…' : 'Look Up'}
        </button>
      </div>

      {/* Results */}
      {data && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs text-muted-foreground">Student ID</p>
              <p className="font-mono text-sm text-foreground">{data.studentId}</p>
            </div>
            <button
              onClick={handlePrint}
              className="px-4 py-2 text-sm rounded border border-border text-muted-foreground hover:bg-muted"
            >
              Print Certificate
            </button>
          </div>

          {data.results.length === 0 ? (
            <p className="text-muted-foreground text-sm">No scored results found for this student.</p>
          ) : (
            <table className="w-full text-sm border-collapse">
              <thead>
                <tr className="border-b text-left">
                  <th className="py-2 pr-4">Exam</th>
                  <th className="py-2 pr-4 text-right">Score</th>
                  <th className="py-2 pr-4 text-right">%</th>
                  <th className="py-2 pr-4">Verified</th>
                  <th className="py-2 pr-4">Scored At</th>
                  <th className="py-2">Hash</th>
                </tr>
              </thead>
              <tbody>
                {data.results.map(r => (
                  <tr key={r.scoreId} className="border-b hover:bg-muted/20">
                    <td className="py-2 pr-4 font-medium text-foreground">{r.examName}</td>
                    <td className="py-2 pr-4 text-right text-muted-foreground">
                      {r.correctAnswers}/{r.totalQuestions}
                    </td>
                    <td className={`py-2 pr-4 text-right font-bold ${pctColor(r.percentage)}`}>
                      {r.percentage.toFixed(1)}%
                    </td>
                    <td className="py-2 pr-4">
                      {r.isVerified ? (
                        <span className="text-xs font-semibold text-green-500">Verified ✓</span>
                      ) : (
                        <span className="text-xs text-muted-foreground">—</span>
                      )}
                    </td>
                    <td className="py-2 pr-4 text-xs text-muted-foreground">
                      {new Date(r.scoredAt).toLocaleDateString()}
                    </td>
                    <td className="py-2 font-mono text-[10px] text-muted-foreground truncate max-w-[120px]">
                      {r.hashHex.substring(0, 16)}…
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  )
}
