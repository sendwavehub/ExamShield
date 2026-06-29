import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import ManualReviewPage from '../pages/ManualReviewPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getPendingReviews: vi.fn(),
    getReviewDetail: vi.fn(),
    submitReview: vi.fn(),
    getCaptureImage: vi.fn(),
  },
}))

const mockReviews = [
  {
    reviewId: 'rev-1',
    captureId: 'cap-1',
    ocrResultId: 'ocr-1',
    createdAt: '2026-06-26T10:00:00Z',
  },
]

const mockDetail = {
  reviewId: 'rev-1',
  captureId: 'cap-1',
  ocrResultId: 'ocr-1',
  status: 'Pending',
  ocrAnswers: [
    { questionNumber: 1, text: 'A', confidence: 0.45 },
    { questionNumber: 2, text: 'B', confidence: 0.92 },
  ],
  createdAt: '2026-06-26T10:00:00Z',
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ManualReviewPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getPendingReviews).mockResolvedValue({ reviews: mockReviews })
  vi.mocked(apiClient.api.getReviewDetail).mockResolvedValue(mockDetail)
  vi.mocked(apiClient.api.submitReview).mockResolvedValue(undefined)
  vi.mocked(apiClient.api.getCaptureImage).mockResolvedValue('blob:mock-capture-image')
})

describe('ManualReviewPage', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /manual review/i })).toBeInTheDocument()
  })

  it('shows pending review count', async () => {
    renderPage()
    expect(await screen.findByText(/1 pending/i)).toBeInTheDocument()
  })

  it('renders a row per pending review', async () => {
    renderPage()
    const rows = await screen.findAllByRole('row')
    expect(rows.length).toBeGreaterThanOrEqual(2) // header + at least 1
  })

  it('loads review detail on row click', async () => {
    renderPage()
    const reviewRow = await screen.findByText('cap-1')
    fireEvent.click(reviewRow)
    expect(await screen.findByText(/ocr answers/i)).toBeInTheDocument()
  })

  it('shows OCR answers with confidence in detail panel', async () => {
    renderPage()
    fireEvent.click(await screen.findByText('cap-1'))
    expect(await screen.findByText('Q1')).toBeInTheDocument()
    expect(await screen.findByText('Q2')).toBeInTheDocument()
  })

  it('shows low-confidence warning for answers below threshold', async () => {
    renderPage()
    fireEvent.click(await screen.findByText('cap-1'))
    await screen.findByText('Q1')
    expect(screen.getByText(/45%/)).toBeInTheDocument()
  })

  it('submit button calls submitReview', async () => {
    renderPage()
    fireEvent.click(await screen.findByText('cap-1'))
    await screen.findByText('Q1')
    const submitBtn = screen.getByRole('button', { name: /submit/i })
    fireEvent.click(submitBtn)
    await waitFor(() => expect(apiClient.api.submitReview).toHaveBeenCalledWith('rev-1', expect.any(Array)))
  })

  it('shows loading state', () => {
    vi.mocked(apiClient.api.getPendingReviews).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows the original capture image in the detail panel', async () => {
    renderPage()
    fireEvent.click(await screen.findByText('cap-1'))
    const img = await screen.findByRole('img', { name: /answer sheet/i })
    expect(img).toHaveAttribute('src', 'blob:mock-capture-image')
  })

  it('shows pixel lock badge in the image panel', async () => {
    renderPage()
    fireEvent.click(await screen.findByText('cap-1'))
    expect(await screen.findByText(/pixel lock/i)).toBeInTheDocument()
  })

  it('fetches capture image using the review captureId', async () => {
    renderPage()
    fireEvent.click(await screen.findByText('cap-1'))
    await screen.findByRole('img', { name: /answer sheet/i })
    expect(apiClient.api.getCaptureImage).toHaveBeenCalledWith('cap-1')
  })
})
