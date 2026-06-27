import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'

export function useScoringQueue() {
  return useQuery({
    queryKey: ['scoring-queue'],
    queryFn: () => api.getScoringQueue(),
    refetchInterval: 30_000,
  })
}

export function useScoreCapture() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (captureId: string) => api.scoreCapture(captureId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scoring-queue'] })
      qc.invalidateQueries({ queryKey: ['results'] })
    },
  })
}

export function useBatchScore() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (examId: string) => api.batchScore(examId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scoring-queue'] })
      qc.invalidateQueries({ queryKey: ['results'] })
    },
  })
}

export function useExportScores() {
  return useMutation({
    mutationFn: (examId?: string) =>
      api.exportScores(examId).then(blob => {
        const url = URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = examId ? `scores-${examId.slice(0, 8)}.csv` : 'scores.csv'
        a.click()
        URL.revokeObjectURL(url)
      }),
  })
}
