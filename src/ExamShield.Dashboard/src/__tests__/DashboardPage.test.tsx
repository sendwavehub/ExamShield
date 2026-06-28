import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DashboardPage from '../pages/DashboardPage'

// ── Hook / API mocks ──────────────────────────────────────────────────────────
vi.mock('../hooks/useDashboardStats', () => ({
  useDashboardStats: vi.fn(() => ({
    data: { totalCaptures: 1024, pendingReview: 12, verifiedToday: 340, activeAlerts: 3 },
    isLoading: false,
    dataUpdatedAt: Date.now(),
  })),
}))

vi.mock('../api/client', () => ({
  api: {
    getStatistics:    vi.fn(() => Promise.resolve({ totalPapersScored: 8, averagePercentage: 87.5, highestScore: 10, lowestScore: 6 })),
    getCaptures:      vi.fn(() => Promise.resolve({ captures: [], totalCount: 0, page: 1, pageSize: 200, totalPages: 0 })),
    getExams:         vi.fn(() => Promise.resolve({ exams: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 0 })),
    getDevices:       vi.fn(() => Promise.resolve({ devices: [] })),
    getSecurityEvents:vi.fn(() => Promise.resolve({ events: [] })),
    getAuditLog:      vi.fn(() => Promise.resolve({ entries: [], totalCount: 0 })),
    getResults:       vi.fn(() => Promise.resolve({ results: [] })),
  },
}))

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <DashboardPage />
    </QueryClientProvider>
  )
}

describe('DashboardPage', () => {
  it('renders the page heading', () => {
    renderPage()
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument()
  })

  it('renders KPI card for total captures', () => {
    renderPage()
    expect(screen.getByText('1,024')).toBeInTheDocument()
  })

  it('renders KPI card for pending review', () => {
    renderPage()
    expect(screen.getByText('12')).toBeInTheDocument()
  })

  it('renders KPI card for verified today', () => {
    renderPage()
    expect(screen.getByText('340')).toBeInTheDocument()
  })

  it('renders alert badge when activeAlerts > 0', () => {
    renderPage()
    expect(screen.getByText(/3 active alert/i)).toBeInTheDocument()
  })

  it('renders secondary KPI labels', () => {
    renderPage()
    expect(screen.getAllByText(/Exams/i).length).toBeGreaterThan(0)
    expect(screen.getByText('Avg Score')).toBeInTheDocument()
    expect(screen.getByText('Active Devices')).toBeInTheDocument()
  })

  it('renders chart section headings', () => {
    renderPage()
    expect(screen.getByText('Capture Pipeline Status')).toBeInTheDocument()
    expect(screen.getByText('Security Threat Breakdown')).toBeInTheDocument()
    expect(screen.getByText('Score Distribution')).toBeInTheDocument()
    expect(screen.getByText('Exam Overview')).toBeInTheDocument()
    expect(screen.getByText('Recent Activity')).toBeInTheDocument()
    expect(screen.getByText('Threat Radar')).toBeInTheDocument()
  })

  it('renders refresh button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /refresh/i })).toBeInTheDocument()
  })

  it('shows empty-state messages when no data', () => {
    renderPage()
    expect(screen.getByText('No captures yet')).toBeInTheDocument()
    expect(screen.getByText('No security events')).toBeInTheDocument()
  })
})
