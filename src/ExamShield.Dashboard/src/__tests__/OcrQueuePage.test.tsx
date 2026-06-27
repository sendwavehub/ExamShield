import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import OcrQueuePage from '../pages/OcrQueuePage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getOcrQueue: vi.fn(),
    triggerOcr: vi.fn(),
  },
}))

const mockQueue = [
  { captureId: 'cap-1', examId: 'exam-1', studentId: 'stu-1', uploadedAt: '2026-06-26T10:00:00Z' },
  { captureId: 'cap-2', examId: 'exam-1', studentId: 'stu-2', uploadedAt: '2026-06-26T11:00:00Z' },
]

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <OcrQueuePage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getOcrQueue).mockResolvedValue({ items: mockQueue })
  vi.mocked(apiClient.api.triggerOcr).mockResolvedValue(undefined)
})

describe('OcrQueuePage', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /ocr queue/i })).toBeInTheDocument()
  })

  it('shows item count in heading area', async () => {
    renderPage()
    expect(await screen.findByText(/2 pending/i)).toBeInTheDocument()
  })

  it('renders a row per queued capture', async () => {
    renderPage()
    const rows = await screen.findAllByRole('row')
    expect(rows).toHaveLength(3) // header + 2 items
  })

  it('displays capture ids', async () => {
    renderPage()
    expect(await screen.findByText('cap-1')).toBeInTheDocument()
    expect(await screen.findByText('cap-2')).toBeInTheDocument()
  })

  it('renders a trigger button per row', async () => {
    renderPage()
    await screen.findByText('cap-1')
    const buttons = screen.getAllByRole('button', { name: /trigger ocr/i })
    expect(buttons).toHaveLength(2)
  })

  it('calls triggerOcr with captureId on button click', async () => {
    renderPage()
    await screen.findByText('cap-1')
    fireEvent.click(screen.getAllByRole('button', { name: /trigger ocr/i })[0])
    await waitFor(() =>
      expect(apiClient.api.triggerOcr).toHaveBeenCalledWith('cap-1')
    )
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.api.getOcrQueue).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows empty message when queue is empty', async () => {
    vi.mocked(apiClient.api.getOcrQueue).mockResolvedValue({ items: [] })
    renderPage()
    expect(await screen.findByText(/no captures/i)).toBeInTheDocument()
  })
})
