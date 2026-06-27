import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import ProfilePage from '../pages/ProfilePage'

vi.mock('../api/client', () => ({
  api: {
    getProfile: vi.fn(),
    getSessions: vi.fn(),
    revokeSession: vi.fn(),
  },
}))

import { api } from '../api/client'
const mockApi = api as {
  getProfile: ReturnType<typeof vi.fn>
  getSessions: ReturnType<typeof vi.fn>
  revokeSession: ReturnType<typeof vi.fn>
}

const profile = { email: 'admin@examshield.io', role: 'Administrator', mfaEnabled: true }
const sessions = {
  sessions: [
    { id: 'sess-1', createdAt: '2026-01-01T00:00:00Z', expiresAt: '2026-01-08T00:00:00Z' },
  ],
}

describe('ProfilePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockApi.getProfile.mockResolvedValue(profile)
    mockApi.getSessions.mockResolvedValue(sessions)
  })

  it('shows user email after loading', async () => {
    render(<MemoryRouter><ProfilePage /></MemoryRouter>)
    expect(await screen.findByText('admin@examshield.io')).toBeInTheDocument()
  })

  it('shows user role after loading', async () => {
    render(<MemoryRouter><ProfilePage /></MemoryRouter>)
    expect(await screen.findByText(/administrator/i)).toBeInTheDocument()
  })

  it('shows MFA enabled badge when MFA is on', async () => {
    render(<MemoryRouter><ProfilePage /></MemoryRouter>)
    expect(await screen.findByText(/mfa enabled/i)).toBeInTheDocument()
  })

  it('shows active sessions list', async () => {
    render(<MemoryRouter><ProfilePage /></MemoryRouter>)
    expect(await screen.findByText(/active sessions/i)).toBeInTheDocument()
    expect(await screen.findByText(/sess-1/i)).toBeInTheDocument()
  })

  it('revoke button calls api.revokeSession', async () => {
    mockApi.revokeSession.mockResolvedValue(undefined)
    mockApi.getSessions
      .mockResolvedValueOnce(sessions)
      .mockResolvedValue({ sessions: [] })
    render(<MemoryRouter><ProfilePage /></MemoryRouter>)
    const revokeBtn = await screen.findByRole('button', { name: /revoke/i })
    await userEvent.click(revokeBtn)
    expect(mockApi.revokeSession).toHaveBeenCalledWith('sess-1')
  })
})
