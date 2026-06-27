import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import VerificationBadge from '../components/ui/VerificationBadge'

describe('VerificationBadge', () => {
  it('shows Verified when status is valid', () => {
    render(<VerificationBadge status="valid" />)
    expect(screen.getByText(/verified/i)).toBeInTheDocument()
  })

  it('shows Tampered when status is invalid', () => {
    render(<VerificationBadge status="invalid" />)
    expect(screen.getByText(/tampered/i)).toBeInTheDocument()
  })

  it('shows Pending when status is pending', () => {
    render(<VerificationBadge status="pending" />)
    expect(screen.getByText(/pending/i)).toBeInTheDocument()
  })

  it('applies green style for valid status', () => {
    const { container } = render(<VerificationBadge status="valid" />)
    expect(container.firstChild).toHaveClass('text-green-500')
  })

  it('applies red style for invalid status', () => {
    const { container } = render(<VerificationBadge status="invalid" />)
    expect(container.firstChild).toHaveClass('text-red-500')
  })
})
