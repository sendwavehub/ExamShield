import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'

export function useReportSummary() {
  return useQuery({
    queryKey: ['report-summary'],
    queryFn: () => api.getReportSummary(),
  })
}
