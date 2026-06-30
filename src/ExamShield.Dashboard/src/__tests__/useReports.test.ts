import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getReportSummary: vi.fn(),
    getExamReport: vi.fn(),
  },
}))

import { api } from '../api/client'
import { useReportSummary, useExamReport } from '../hooks/useReports'

const mockApi = api as {
  getReportSummary: ReturnType<typeof vi.fn>
  getExamReport: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useReportSummary', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns summary data on success', async () => {
    const mockData = { totalExams: 10, totalStudents: 300 }
    mockApi.getReportSummary.mockResolvedValue(mockData)
    const { result } = renderHook(() => useReportSummary(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getReportSummary.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useReportSummary(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getReportSummary.mockRejectedValue(new Error('error'))
    const { result } = renderHook(() => useReportSummary(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useExamReport', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('is disabled when examId is null', () => {
    mockApi.getExamReport.mockResolvedValue({})
    const { result } = renderHook(() => useExamReport(null), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
    expect(mockApi.getExamReport).not.toHaveBeenCalled()
  })

  it('fetches exam report when examId is provided', async () => {
    const mockReport = { examId: 'exam-1', passRate: 0.85 }
    mockApi.getExamReport.mockResolvedValue(mockReport)
    const { result } = renderHook(() => useExamReport('exam-1'), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockReport)
    expect(mockApi.getExamReport).toHaveBeenCalledWith('exam-1')
  })

  it('sets error state on failure', async () => {
    mockApi.getExamReport.mockRejectedValue(new Error('not found'))
    const { result } = renderHook(() => useExamReport('exam-bad'), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
