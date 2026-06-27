import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ScoringPage from '../pages/ScoringPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getScoringQueue: vi.fn(),
    scoreCapture: vi.fn(),
  },
}))

const mockQueue = [
  {
    captureId: 'cap-1',
    examId: 'exam-1',
    ocrResultId: 'ocr-1',
    ocrStatus: 'Completed',
    overallConfidence: 0.92,
    completedAt: '2026-06-26T10:00:00Z',
  },
  {
    captureId: 'cap-2',
    examId: 'exam-1',
    ocrResultId: 'ocr-2',
    ocrStatus: 'Completed',
    overallConfidence: 0.75,
    completedAt: '2026-06-26T11:00:00Z',
  },
]

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <ScoringPage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getScoringQueue).mockResolvedValue({ items: mockQueue })
  vi.mocked(apiClient.api.scoreCapture).mockResolvedValue({
    scoreId: 'score-new',
    correctAnswers: 45,
    totalQuestions: 50,
    percentage: 90.0,
  })
})

describe('ScoringPage', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /scoring/i })).toBeInTheDocument()
  })

  it('shows pending count', async () => {
    renderPage()
    expect(await screen.findByText(/2 pending/i)).toBeInTheDocument()
  })

  it('renders a row per item', async () => {
    renderPage()
    const rows = await screen.findAllByRole('row')
    expect(rows).toHaveLength(3) // header + 2
  })

  it('displays capture ids', async () => {
    renderPage()
    expect(await screen.findByText('cap-1')).toBeInTheDocument()
    expect(await screen.findByText('cap-2')).toBeInTheDocument()
  })

  it('shows confidence values', async () => {
    renderPage()
    expect(await screen.findByText('92%')).toBeInTheDocument()
    expect(await screen.findByText('75%')).toBeInTheDocument()
  })

  it('renders a score button per row', async () => {
    renderPage()
    await screen.findByText('cap-1')
    const buttons = screen.getAllByRole('button', { name: /^score$/i })
    expect(buttons).toHaveLength(2)
  })

  it('calls scoreCapture on button click', async () => {
    renderPage()
    await screen.findByText('cap-1')
    fireEvent.click(screen.getAllByRole('button', { name: /^score$/i })[0])
    await waitFor(() =>
      expect(apiClient.api.scoreCapture).toHaveBeenCalledWith('cap-1')
    )
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.api.getScoringQueue).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows empty state when queue is empty', async () => {
    vi.mocked(apiClient.api.getScoringQueue).mockResolvedValue({ items: [] })
    renderPage()
    expect(await screen.findByText(/no captures/i)).toBeInTheDocument()
  })
})
