import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, type PendingReviewItem, type ReviewDetailResponse, type OcrAnswer } from '../api/client'
import { AlertTriangle, CheckCircle, Send } from 'lucide-react'
import { cn } from '../lib/utils'

const LOW_CONFIDENCE_THRESHOLD = 0.6

function confidenceColor(confidence: number) {
  if (confidence >= LOW_CONFIDENCE_THRESHOLD) return 'text-green-500'
  return 'text-yellow-500'
}

function AnswerRow({ answer, selected, onSelect }: {
  answer: OcrAnswer
  selected: string
  onSelect: (text: string) => void
}) {
  const isLow = answer.confidence < LOW_CONFIDENCE_THRESHOLD
  const pct = Math.round(answer.confidence * 100)
  return (
    <tr className="border-b border-border last:border-0">
      <td className="px-4 py-3 font-medium text-foreground">Q{answer.questionNumber}</td>
      <td className="px-4 py-3">
        <input
          type="text"
          value={selected}
          onChange={e => onSelect(e.target.value)}
          className="w-24 rounded border border-border bg-card px-2 py-1 text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
          placeholder={answer.text}
        />
      </td>
      <td className={cn('px-4 py-3 text-sm', confidenceColor(answer.confidence))}>
        {pct}%{isLow && <AlertTriangle className="ml-1 inline h-3 w-3" />}
      </td>
    </tr>
  )
}

function ReviewDetailPanel({ detail, onSubmit, isPending }: {
  detail: ReviewDetailResponse
  onSubmit: (answers: { questionNumber: number; text: string }[]) => void
  isPending: boolean
}) {
  const [overrides, setOverrides] = useState<Record<number, string>>(() =>
    Object.fromEntries(detail.ocrAnswers.map(a => [a.questionNumber, a.text]))
  )

  const answers = detail.ocrAnswers.map(a => ({
    questionNumber: a.questionNumber,
    text: overrides[a.questionNumber] ?? a.text,
  }))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-foreground">OCR Answers</h2>
        <span className="text-xs text-muted-foreground font-mono truncate max-w-[200px]">{detail.captureId}</span>
      </div>

      <div className="overflow-hidden rounded-xl border border-border">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              {['Question', 'Answer', 'Confidence'].map(h => (
                <th key={h} className="px-4 py-2 text-left text-xs font-medium text-muted-foreground">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {detail.ocrAnswers.map(a => (
              <AnswerRow
                key={a.questionNumber}
                answer={a}
                selected={overrides[a.questionNumber] ?? a.text}
                onSelect={v => setOverrides(prev => ({ ...prev, [a.questionNumber]: v }))}
              />
            ))}
          </tbody>
        </table>
      </div>

      <button
        onClick={() => onSubmit(answers)}
        disabled={isPending}
        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50"
      >
        <Send className="h-4 w-4" />
        {isPending ? 'Submitting…' : 'Submit Review'}
      </button>
    </div>
  )
}

export default function ManualReviewPage() {
  const qc = useQueryClient()
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const { data: listData, isLoading } = useQuery({
    queryKey: ['reviews'],
    queryFn: api.getPendingReviews,
    refetchInterval: 15_000,
  })

  const { data: detail } = useQuery({
    queryKey: ['review', selectedId],
    queryFn: () => api.getReviewDetail(selectedId!),
    enabled: !!selectedId,
  })

  const { mutate: submit, isPending: isSubmitting } = useMutation({
    mutationFn: ({ id, answers }: { id: string; answers: { questionNumber: number; text: string }[] }) =>
      api.submitReview(id, answers),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['reviews'] })
      setSelectedId(null)
    },
  })

  const pendingCount = listData?.reviews.length ?? 0

  return (
    <div className="flex h-full gap-6">
      {/* Left: pending list */}
      <div className="w-80 shrink-0 space-y-4">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold text-foreground">Manual Review</h1>
          {listData && (
            <span className="rounded-full bg-yellow-500/10 px-2 py-0.5 text-xs font-medium text-yellow-500">
              {pendingCount} pending
            </span>
          )}
        </div>

        {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}

        {listData && (
          <div className="overflow-hidden rounded-xl border border-border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="px-4 py-2 text-left text-xs font-medium text-muted-foreground">Capture</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-muted-foreground">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {listData.reviews.map((r: PendingReviewItem) => (
                  <tr
                    key={r.reviewId}
                    onClick={() => setSelectedId(r.reviewId)}
                    className={cn(
                      'cursor-pointer transition-colors hover:bg-muted/40',
                      selectedId === r.reviewId && 'bg-primary/10'
                    )}
                  >
                    <td className="px-4 py-3 font-mono text-xs text-foreground">{r.captureId}</td>
                    <td className="px-4 py-3 text-xs text-muted-foreground">
                      {new Date(r.createdAt).toLocaleDateString()}
                    </td>
                  </tr>
                ))}
                {listData.reviews.length === 0 && (
                  <tr>
                    <td colSpan={2} className="px-4 py-6 text-center text-muted-foreground">
                      <CheckCircle className="mx-auto mb-1 h-5 w-5 text-green-500" />
                      All reviews complete
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Right: detail panel */}
      <div className="flex-1">
        {detail && selectedId ? (
          <ReviewDetailPanel
            detail={detail}
            onSubmit={answers => submit({ id: selectedId, answers })}
            isPending={isSubmitting}
          />
        ) : (
          <div className="flex h-40 items-center justify-center rounded-xl border border-dashed border-border text-muted-foreground">
            Select a review to begin
          </div>
        )}
      </div>
    </div>
  )
}
