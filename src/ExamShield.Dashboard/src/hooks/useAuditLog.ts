import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'

export function useAuditLog(page: number, pageSize = 20) {
  return useQuery({
    queryKey: ['audit', page, pageSize],
    queryFn: () => api.getAuditLog({ page, pageSize }),
    placeholderData: prev => prev,
  })
}
