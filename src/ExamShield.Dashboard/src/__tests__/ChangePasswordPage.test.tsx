import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import ChangePasswordPage from '../pages/ChangePasswordPage'

vi.mock('../api/client', () => ({
  api: { changePassword: vi.fn() },
}))

import { api } from '../api/client'
const mockApi = api as { changePassword: ReturnType<typeof vi.fn> }

describe('ChangePasswordPage', () => {
  beforeEach(() => { vi.clearAllMocks() })

  it('renders current password and new password fields', () => {
    render(<MemoryRouter><ChangePasswordPage /></MemoryRouter>)
    expect(screen.getByLabelText(/current password/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/new password/i)).toBeInTheDocument()
  })

  it('renders a change password button', () => {
    render(<MemoryRouter><ChangePasswordPage /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /change password/i })).toBeInTheDocument()
  })

  it('shows validation error when fields are empty', async () => {
    render(<MemoryRouter><ChangePasswordPage /></MemoryRouter>)
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(await screen.findByText(/current password is required/i)).toBeInTheDocument()
  })

  it('calls api.changePassword with entered values', async () => {
    mockApi.changePassword.mockResolvedValue(undefined)
    render(<MemoryRouter><ChangePasswordPage /></MemoryRouter>)
    await userEvent.type(screen.getByLabelText(/current password/i), 'OldPass@1')
    await userEvent.type(screen.getByLabelText(/new password/i), 'NewPass@1')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(mockApi.changePassword).toHaveBeenCalledWith('OldPass@1', 'NewPass@1')
  })

  it('shows success message on completion', async () => {
    mockApi.changePassword.mockResolvedValue(undefined)
    render(<MemoryRouter><ChangePasswordPage /></MemoryRouter>)
    await userEvent.type(screen.getByLabelText(/current password/i), 'OldPass@1')
    await userEvent.type(screen.getByLabelText(/new password/i), 'NewPass@1')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(await screen.findByText(/password changed/i)).toBeInTheDocument()
  })

  it('shows error message when current password is incorrect', async () => {
    mockApi.changePassword.mockRejectedValue(new Error('401'))
    render(<MemoryRouter><ChangePasswordPage /></MemoryRouter>)
    await userEvent.type(screen.getByLabelText(/current password/i), 'WrongPass@1')
    await userEvent.type(screen.getByLabelText(/new password/i), 'NewPass@1')
    await userEvent.click(screen.getByRole('button', { name: /change password/i }))
    expect(await screen.findByText(/current password is incorrect/i)).toBeInTheDocument()
  })
})
