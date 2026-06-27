import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, type CreateExamPayload } from '../api/client'

export function useExams() {
  return useQuery({
    queryKey: ['exams'],
    queryFn: () => api.getExams(),
  })
}

export function useCreateExam() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateExamPayload) => api.createExam(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['exams'] }),
  })
}
