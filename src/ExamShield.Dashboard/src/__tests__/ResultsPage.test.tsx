import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ResultsPage from '../pages/ResultsPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getResults: vi.fn(),
    getStatistics: vi.fn(),
  },
}))

const mockResults = [
  {
    scoreId: 'score-1',
    captureId: 'cap-1',
    examId: 'exam-1',
    studentId: 'stu-1',
    correctAnswers: 45,
    totalQuestions: 50,
    percentage: 90.0,
    scoredAt: '2026-06-26T10:00:00Z',
  },
  {
    scoreId: 'score-2',
    captureId: 'cap-2',
    examId: 'exam-1',
    studentId: 'stu-2',
    correctAnswers: 30,
    totalQuestions: 50,
    percentage: 60.0,
    scoredAt: '2026-06-26T11:00:00Z',
  },
]

const mockStats = {
  totalPapersScored: 2,
  averagePercentage: 75.0,
  highestScore: 90,
  lowestScore: 60,
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <ResultsPage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getResults).mockResolvedValue({ results: mockResults })
  vi.mocked(apiClient.api.getStatistics).mockResolvedValue(mockStats)
})

describe('ResultsPage', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /results/i })).toBeInTheDocument()
  })

  it('displays summary stats', async () => {
    renderPage()
    expect(await screen.findByText('75.0%')).toBeInTheDocument()
    expect(await screen.findByText('Total Scored')).toBeInTheDocument()
  })

  it('renders a row per result', async () => {
    renderPage()
    const rows = await screen.findAllByRole('row')
    expect(rows).toHaveLength(3) // header + 2 data rows
  })

  it('displays student ids', async () => {
    renderPage()
    expect(await screen.findByText('stu-1')).toBeInTheDocument()
    expect(await screen.findByText('stu-2')).toBeInTheDocument()
  })

  it('displays percentage values', async () => {
    renderPage()
    expect(await screen.findByText('90.0%')).toBeInTheDocument()
    expect(await screen.findByText('60.0%')).toBeInTheDocument()
  })

  it('shows score fractions', async () => {
    renderPage()
    expect(await screen.findByText('45 / 50')).toBeInTheDocument()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.api.getResults).mockImplementation(() => new Promise(() => {}))
    vi.mocked(apiClient.api.getStatistics).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows empty message when no results', async () => {
    vi.mocked(apiClient.api.getResults).mockResolvedValue({ results: [] })
    renderPage()
    expect(await screen.findByText(/no results/i)).toBeInTheDocument()
  })
})
