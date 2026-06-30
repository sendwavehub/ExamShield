import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getExams: vi.fn(),
    createExam: vi.fn(),
    deleteExam: vi.fn(),
    updateExam: vi.fn(),
    activateExam: vi.fn(),
    closeExam: vi.fn(),
    getAnswerKey: vi.fn(),
    setAnswerKey: vi.fn(),
    getExamCandidates: vi.fn(),
    getExamSubmissionStatus: vi.fn(),
    enrollStudent: vi.fn(),
    unenrollStudent: vi.fn(),
  },
}))

import { api } from '../api/client'
import {
  useExams,
  useCreateExam,
  useDeleteExam,
  useUpdateExam,
  useActivateExam,
  useCloseExam,
  useAnswerKey,
  useSetAnswerKey,
  useExamCandidates,
  useExamSubmissionStatus,
  useEnrollStudent,
  useUnenrollStudent,
} from '../hooks/useExams'

const mockApi = api as Record<string, ReturnType<typeof vi.fn>>

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockExams = { items: [{ id: 'exam-1', name: 'Math Final' }], total: 1 }

describe('useExams', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns exam list on success', async () => {
    mockApi.getExams.mockResolvedValue(mockExams)
    const { result } = renderHook(() => useExams(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockExams)
  })

  it('starts in loading state', () => {
    mockApi.getExams.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useExams(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getExams.mockRejectedValue(new Error('error'))
    const { result } = renderHook(() => useExams(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })

  it('passes filters to api.getExams', async () => {
    mockApi.getExams.mockResolvedValue(mockExams)
    renderHook(() => useExams(1, 50, 'math', 'Active'), { wrapper })
    await waitFor(() => expect(mockApi.getExams).toHaveBeenCalledWith(1, 50, 'math', 'Active', undefined, undefined))
  })
})

describe('useCreateExam', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.createExam with payload', async () => {
    const payload = { name: 'New Exam', description: 'desc' }
    mockApi.createExam.mockResolvedValue({ id: 'exam-new', ...payload })
    mockApi.getExams.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useCreateExam(), { wrapper })
    result.current.mutate(payload as any)
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.createExam).toHaveBeenCalledWith(payload)
  })
})

describe('useDeleteExam', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.deleteExam with exam id', async () => {
    mockApi.deleteExam.mockResolvedValue(undefined)
    mockApi.getExams.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useDeleteExam(), { wrapper })
    result.current.mutate('exam-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.deleteExam).toHaveBeenCalledWith('exam-1')
  })
})

describe('useUpdateExam', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.updateExam with examId and payload', async () => {
    const payload = { name: 'Updated', description: 'new desc' }
    mockApi.updateExam.mockResolvedValue({ id: 'exam-1', ...payload })
    mockApi.getExams.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useUpdateExam(), { wrapper })
    result.current.mutate({ examId: 'exam-1', payload })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.updateExam).toHaveBeenCalledWith('exam-1', payload)
  })
})

describe('useActivateExam', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.activateExam with exam id', async () => {
    mockApi.activateExam.mockResolvedValue({ status: 'Active' })
    mockApi.getExams.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useActivateExam(), { wrapper })
    result.current.mutate('exam-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.activateExam).toHaveBeenCalledWith('exam-1')
  })
})

describe('useCloseExam', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.closeExam with exam id', async () => {
    mockApi.closeExam.mockResolvedValue({ status: 'Closed' })
    mockApi.getExams.mockResolvedValue({ items: [], total: 0 })
    const { result } = renderHook(() => useCloseExam(), { wrapper })
    result.current.mutate('exam-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.closeExam).toHaveBeenCalledWith('exam-1')
  })
})

describe('useAnswerKey', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('is disabled when examId is null', () => {
    mockApi.getAnswerKey.mockResolvedValue({})
    const { result } = renderHook(() => useAnswerKey(null), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
    expect(mockApi.getAnswerKey).not.toHaveBeenCalled()
  })

  it('fetches answer key when examId is provided', async () => {
    const mockKey = { answers: { 1: 'A', 2: 'B' } }
    mockApi.getAnswerKey.mockResolvedValue(mockKey)
    const { result } = renderHook(() => useAnswerKey('exam-1'), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockKey)
    expect(mockApi.getAnswerKey).toHaveBeenCalledWith('exam-1')
  })
})

describe('useSetAnswerKey', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.setAnswerKey with examId and answers', async () => {
    const answers = { 1: 'A', 2: 'C' }
    mockApi.setAnswerKey.mockResolvedValue({ saved: true })
    mockApi.getAnswerKey.mockResolvedValue({})
    const { result } = renderHook(() => useSetAnswerKey(), { wrapper })
    result.current.mutate({ examId: 'exam-1', answers })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.setAnswerKey).toHaveBeenCalledWith('exam-1', answers)
  })
})

describe('useExamCandidates', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('is disabled when examId is null', () => {
    mockApi.getExamCandidates.mockResolvedValue([])
    const { result } = renderHook(() => useExamCandidates(null), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
  })

  it('fetches candidates when examId is provided', async () => {
    const candidates = [{ studentId: 'stu-1', name: 'Alice' }]
    mockApi.getExamCandidates.mockResolvedValue(candidates)
    const { result } = renderHook(() => useExamCandidates('exam-1'), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(candidates)
  })
})

describe('useExamSubmissionStatus', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('is disabled when examId is null', () => {
    mockApi.getExamSubmissionStatus.mockResolvedValue({})
    const { result } = renderHook(() => useExamSubmissionStatus(null), { wrapper })
    expect(result.current.fetchStatus).toBe('idle')
  })

  it('fetches submission status when examId provided', async () => {
    const status = { submitted: 10, total: 20 }
    mockApi.getExamSubmissionStatus.mockResolvedValue(status)
    const { result } = renderHook(() => useExamSubmissionStatus('exam-1'), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(status)
  })
})

describe('useEnrollStudent', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.enrollStudent with examId and studentId', async () => {
    mockApi.enrollStudent.mockResolvedValue({ enrolled: true })
    mockApi.getExamCandidates.mockResolvedValue([])
    const { result } = renderHook(() => useEnrollStudent(), { wrapper })
    result.current.mutate({ examId: 'exam-1', studentId: 'stu-1' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.enrollStudent).toHaveBeenCalledWith('exam-1', 'stu-1')
  })
})

describe('useUnenrollStudent', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.unenrollStudent with examId and studentId', async () => {
    mockApi.unenrollStudent.mockResolvedValue({ unenrolled: true })
    mockApi.getExamCandidates.mockResolvedValue([])
    const { result } = renderHook(() => useUnenrollStudent(), { wrapper })
    result.current.mutate({ examId: 'exam-1', studentId: 'stu-2' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.unenrollStudent).toHaveBeenCalledWith('exam-1', 'stu-2')
  })
})
