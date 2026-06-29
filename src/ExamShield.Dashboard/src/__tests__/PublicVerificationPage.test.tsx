import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import PublicVerificationPage from '../pages/PublicVerificationPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    publicVerify: vi.fn(),
    publicVerifyByHash: vi.fn(),
  },
}))

const validResult = {
  captureId: 'cap-1',
  isValid: true,
  hashValid: true,
  signatureValid: true,
  isUploaded: true,
  capturedAt: '2026-06-26T10:00:00Z',
}

const tamperedResult = {
  captureId: 'cap-bad',
  isValid: false,
  hashValid: false,
  signatureValid: true,
  isUploaded: true,
  capturedAt: '2026-06-26T10:00:00Z',
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/public/verify']}>
        <PublicVerificationPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(apiClient.api.publicVerify).mockResolvedValue(validResult)
  vi.mocked(apiClient.api.publicVerifyByHash).mockResolvedValue(validResult)
})

describe('PublicVerificationPage — layout', () => {
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

  it('shows Capture ID and SHA-256 Hash mode tabs', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /capture id/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sha-256 hash/i })).toBeInTheDocument()
  })

  it('default placeholder is for Capture ID mode', () => {
    renderPage()
    expect(screen.getByPlaceholderText(/capture id/i)).toBeInTheDocument()
  })
})

describe('PublicVerificationPage — Capture ID mode', () => {
  it('calls publicVerify with the entered capture ID on submit', async () => {
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await waitFor(() => expect(apiClient.api.publicVerify).toHaveBeenCalledWith('cap-1'))
  })

  it('pressing Enter triggers verify', async () => {
    renderPage()
    const input = screen.getByRole('textbox')
    fireEvent.change(input, { target: { value: 'cap-1' } })
    fireEvent.keyDown(input, { key: 'Enter' })
    await waitFor(() => expect(apiClient.api.publicVerify).toHaveBeenCalledWith('cap-1'))
  })

  it('does not call API when input is empty', async () => {
    renderPage()
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await new Promise(r => setTimeout(r, 50))
    expect(apiClient.api.publicVerify).not.toHaveBeenCalled()
  })

  it('shows verified result for a valid capture', async () => {
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByText(/verified.*authentic/i)).toBeInTheDocument()
  })

  it('shows tampered result for an invalid capture', async () => {
    vi.mocked(apiClient.api.publicVerify).mockResolvedValue(tamperedResult)
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-bad' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByText(/tampered/i)).toBeInTheDocument()
  })

  it('shows error alert when capture is not found', async () => {
    vi.mocked(apiClient.api.publicVerify).mockRejectedValue(new Error('404: Not found'))
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'bad-id' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })

  it('error alert contains the error message', async () => {
    vi.mocked(apiClient.api.publicVerify).mockRejectedValue(new Error('Capture not found'))
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'x' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByText(/capture not found/i)).toBeInTheDocument()
  })
})

describe('PublicVerificationPage — SHA-256 hash mode', () => {
  it('switching to hash mode changes the placeholder', async () => {
    const user = userEvent.setup()
    renderPage()
    await user.click(screen.getByRole('button', { name: /sha-256 hash/i }))
    expect(screen.getByPlaceholderText(/sha-256/i)).toBeInTheDocument()
  })

  it('calls publicVerifyByHash (not publicVerify) in hash mode', async () => {
    const user = userEvent.setup()
    renderPage()
    await user.click(screen.getByRole('button', { name: /sha-256 hash/i }))
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'deadbeef'.repeat(8) } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await waitFor(() => expect(apiClient.api.publicVerifyByHash).toHaveBeenCalled())
    expect(apiClient.api.publicVerify).not.toHaveBeenCalled()
  })

  it('switching mode clears the input', async () => {
    const user = userEvent.setup()
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    await user.click(screen.getByRole('button', { name: /sha-256 hash/i }))
    expect(screen.getByRole('textbox')).toHaveValue('')
  })
})

describe('PublicVerificationPage — result panel', () => {
  it('shows capture ID in result panel', async () => {
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    expect(await screen.findByText('cap-1')).toBeInTheDocument()
  })

  it('shows hash valid status', async () => {
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await screen.findByText(/verified/i)
    // Both hash and signature show ✓ Valid when both pass
    const valids = screen.getAllByText(/✓ valid/i)
    expect(valids.length).toBeGreaterThanOrEqual(1)
  })

  it('shows hash mismatch status when hashValid is false', async () => {
    vi.mocked(apiClient.api.publicVerify).mockResolvedValue({ ...validResult, isValid: false, hashValid: false })
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-bad' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await screen.findByText(/tampered/i)
    expect(screen.getByText(/mismatch/i)).toBeInTheDocument()
  })

  it('shows captured date when capturedAt is present', async () => {
    renderPage()
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'cap-1' } })
    fireEvent.click(screen.getByRole('button', { name: /verify/i }))
    await screen.findByText(/verified/i)
    // Date is locale-formatted — just check that the "Captured" label appears
    expect(screen.getByText(/captured/i)).toBeInTheDocument()
  })
})
