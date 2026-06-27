import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import CaptureSessionsPage from '../pages/CaptureSessionsPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getCaptures: vi.fn(),
    verifyCapture: vi.fn(),
  },
}))

const mockCaptures = [
  {
    captureId: 'cap-1',
    examId: 'exam-1',
    studentId: 'stu-1',
    deviceId: 'dev-1',
    status: 'Verified',
    capturedAt: '2026-06-26T10:00:00Z',
    storageKey: 'storage/cap-1.jpg',
  },
  {
    captureId: 'cap-2',
    examId: 'exam-1',
    studentId: 'stu-2',
    deviceId: 'dev-1',
    status: 'Uploaded',
    capturedAt: '2026-06-26T11:00:00Z',
    storageKey: 'storage/cap-2.jpg',
  },
  {
    captureId: 'cap-3',
    examId: 'exam-1',
    studentId: 'stu-3',
    deviceId: 'dev-2',
    status: 'Tampered',
    capturedAt: '2026-06-26T12:00:00Z',
    storageKey: null,
  },
]

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <CaptureSessionsPage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getCaptures).mockResolvedValue({ captures: mockCaptures })
  vi.mocked(apiClient.api.verifyCapture).mockResolvedValue({ isValid: true, hashValid: true, signatureValid: true })
})

describe('CaptureSessionsPage', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /capture sessions/i })).toBeInTheDocument()
  })

  it('shows total capture count', async () => {
    renderPage()
    expect(await screen.findByText(/3 captures/i)).toBeInTheDocument()
  })

  it('renders a row per capture', async () => {
    renderPage()
    const rows = await screen.findAllByRole('row')
    expect(rows).toHaveLength(4) // header + 3
  })

  it('displays capture ids', async () => {
    renderPage()
    expect(await screen.findByText('cap-1')).toBeInTheDocument()
    expect(await screen.findByText('cap-2')).toBeInTheDocument()
    expect(await screen.findByText('cap-3')).toBeInTheDocument()
  })

  it('shows status chips for each capture', async () => {
    renderPage()
    await screen.findByText('cap-1')
    expect(screen.getByText('Verified')).toBeInTheDocument()
    expect(screen.getByText('Uploaded')).toBeInTheDocument()
    expect(screen.getByText('Tampered')).toBeInTheDocument()
  })

  it('shows verify button for uploaded captures', async () => {
    renderPage()
    await screen.findByText('cap-1')
    const verifyBtns = screen.getAllByRole('button', { name: /verify/i })
    expect(verifyBtns.length).toBeGreaterThanOrEqual(1)
  })

  it('calls verifyCapture on button click', async () => {
    renderPage()
    await screen.findByText('cap-1')
    fireEvent.click(screen.getAllByRole('button', { name: /verify/i })[0])
    await waitFor(() =>
      expect(apiClient.api.verifyCapture).toHaveBeenCalled()
    )
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.api.getCaptures).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows empty state when no captures', async () => {
    vi.mocked(apiClient.api.getCaptures).mockResolvedValue({ captures: [] })
    renderPage()
    expect(await screen.findByText(/no captures/i)).toBeInTheDocument()
  })
})
