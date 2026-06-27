import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import DashboardPage from '../pages/DashboardPage'

const mockStats = {
  totalCaptures: 1024,
  pendingReview: 12,
  verifiedToday: 340,
  alertCount: 3,
}

describe('DashboardPage', () => {
  it('renders the page heading', () => {
    render(<DashboardPage stats={mockStats} />)
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument()
  })

  it('renders a KPI card for total captures', () => {
    render(<DashboardPage stats={mockStats} />)
    expect(screen.getByText('1,024')).toBeInTheDocument()
  })

  it('renders a KPI card for pending review', () => {
    render(<DashboardPage stats={mockStats} />)
    expect(screen.getByText('12')).toBeInTheDocument()
  })

  it('renders an alert indicator when alertCount > 0', () => {
    render(<DashboardPage stats={mockStats} />)
    expect(screen.getByText(/3 alert/i)).toBeInTheDocument()
  })

  it('renders no alert badge when alertCount is 0', () => {
    render(<DashboardPage stats={{ ...mockStats, alertCount: 0 }} />)
    expect(screen.queryByText(/\d+ alert/i)).not.toBeInTheDocument()
  })
})
