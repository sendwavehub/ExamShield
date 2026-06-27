import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import PublicVerificationPage from '../pages/PublicVerificationPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    publicVerify: vi.fn(),
  },
}))

function renderPage(initialPath = '/public/verify') {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[initialPath]}>
        <PublicVerificationPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('PublicVerificationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders a heading', () => {
    renderPage()
    expect(screen.getByRole('heading', { name: /verify/i })).toBeInTheDocument()
  })

  it('renders a capture ID input', () => {
    renderPage()
    expect(screen.getByRole('textbox')).toBeInTheDocument()
  })

  it('renders a verify button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /verify/i })).toBeInTheDocument()
  })

  it('calls publicVerify with the entered capture ID on submit', async () => {
    vi.mocked(apiClient.api.publicVerify).mockResolvedValue({
      captureId: 'cap-1',
      isValid: true,
      hashValid: true,
      signatureValid: true,
      isUploaded: true,
      capturedAt: '2026-06-26T10:00:00Z',
    })
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await waitFor(() => expect(apiClient.api.publicVerify).toHaveBeenCalledWith('cap-1'))
  })

  it('shows verified result for a valid capture', async () => {
    vi.mocked(apiClient.api.publicVerify).mockResolvedValue({
      captureId: 'cap-1',
      isValid: true,
      hashValid: true,
      signatureValid: true,
      isUploaded: true,
      capturedAt: '2026-06-26T10:00:00Z',
    })
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByText(/verified/i)).toBeInTheDocument()
  })

  it('shows tampered result for an invalid capture', async () => {
    vi.mocked(apiClient.api.publicVerify).mockResolvedValue({
      captureId: 'cap-bad',
      isValid: false,
      hashValid: false,
      signatureValid: true,
      isUploaded: true,
      capturedAt: '2026-06-26T10:00:00Z',
    })
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-bad' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByText(/tampered/i)).toBeInTheDocument()
  })

  it('shows error message when capture not found', async () => {
    vi.mocked(apiClient.api.publicVerify).mockRejectedValue(new Error('404: Not found'))
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'bad-id' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })
})
