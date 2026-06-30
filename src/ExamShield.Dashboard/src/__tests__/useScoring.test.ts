import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

vi.mock('../api/client', () => ({
  api: {
    getScoringQueue: vi.fn(),
    scoreCapture: vi.fn(),
    batchScore: vi.fn(),
    exportScores: vi.fn(),
  },
}))

import { api } from '../api/client'
import { useScoringQueue, useScoreCapture, useBatchScore, useExportScores } from '../hooks/useScoring'

const mockApi = api as {
  getScoringQueue: ReturnType<typeof vi.fn>
  scoreCapture: ReturnType<typeof vi.fn>
  batchScore: ReturnType<typeof vi.fn>
  exportScores: ReturnType<typeof vi.fn>
}

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return React.createElement(QueryClientProvider, { client: qc }, children)
}

describe('useScoringQueue', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('returns scoring queue data on success', async () => {
    const mockData = { items: [{ captureId: 'cap-1', ocrStatus: 'Completed' }] }
    mockApi.getScoringQueue.mockResolvedValue(mockData)
    const { result } = renderHook(() => useScoringQueue(), { wrapper })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toEqual(mockData)
  })

  it('starts in loading state', () => {
    mockApi.getScoringQueue.mockImplementation(() => new Promise(() => {}))
    const { result } = renderHook(() => useScoringQueue(), { wrapper })
    expect(result.current.isPending).toBe(true)
  })

  it('sets error state on failure', async () => {
    mockApi.getScoringQueue.mockRejectedValue(new Error('error'))
    const { result } = renderHook(() => useScoringQueue(), { wrapper })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useScoreCapture', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.scoreCapture with captureId', async () => {
    mockApi.scoreCapture.mockResolvedValue({ scoreId: 'sc-1', correctAnswers: 45 })
    mockApi.getScoringQueue.mockResolvedValue({ items: [] })
    const { result } = renderHook(() => useScoreCapture(), { wrapper })
    result.current.mutate('cap-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.scoreCapture).toHaveBeenCalledWith('cap-1')
  })

  it('sets isError on failure', async () => {
    mockApi.scoreCapture.mockRejectedValue(new Error('cannot score'))
    const { result } = renderHook(() => useScoreCapture(), { wrapper })
    result.current.mutate('cap-bad')
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})

describe('useBatchScore', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('calls api.batchScore with examId', async () => {
    mockApi.batchScore.mockResolvedValue({ scored: 5, skipped: 0 })
    mockApi.getScoringQueue.mockResolvedValue({ items: [] })
    const { result } = renderHook(() => useBatchScore(), { wrapper })
    result.current.mutate('exam-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.batchScore).toHaveBeenCalledWith('exam-1')
  })
})

describe('useExportScores', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    const createObjectURL = vi.fn().mockReturnValue('blob:mock')
    const revokeObjectURL = vi.fn()
    Object.defineProperty(window, 'URL', { value: { createObjectURL, revokeObjectURL }, writable: true })
    // Suppress jsdom click errors
    const origClick = HTMLAnchorElement.prototype.click
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
  })

  it('calls api.exportScores without examId by default', async () => {
    mockApi.exportScores.mockResolvedValue(new Blob(['csv'], { type: 'text/csv' }))
    const { result } = renderHook(() => useExportScores(), { wrapper })
    result.current.mutate(undefined)
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.exportScores).toHaveBeenCalledWith(undefined)
  })

  it('calls api.exportScores with examId when provided', async () => {
    mockApi.exportScores.mockResolvedValue(new Blob(['csv'], { type: 'text/csv' }))
    const { result } = renderHook(() => useExportScores(), { wrapper })
    result.current.mutate('exam-1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(mockApi.exportScores).toHaveBeenCalledWith('exam-1')
  })

  it('sets isError when export fails', async () => {
    mockApi.exportScores.mockRejectedValue(new Error('export error'))
    const { result } = renderHook(() => useExportScores(), { wrapper })
    result.current.mutate('exam-fail')
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
