import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import LoginHistoryTable from '../components/LoginHistoryTable'
import type { LoginHistoryEntry } from '../api/client'

const makeEntries = (overrides: Partial<LoginHistoryEntry>[] = []): LoginHistoryEntry[] =>
  overrides.map((o, i) => ({
    id: `id-${i}`,
    eventType: 'LoginSuccess',
    userId: 'user-1',
    ipAddress: '127.0.0.1',
    occurredAt: '2026-01-01T00:00:00Z',
    ...o,
  }))

describe('LoginHistoryTable', () => {
  it('renders column headers', () => {
    render(<LoginHistoryTable entries={[]} />)
    expect(screen.getByText(/event/i)).toBeInTheDocument()
    expect(screen.getByText(/ip address/i)).toBeInTheDocument()
    expect(screen.getByText(/occurred/i)).toBeInTheDocument()
  })

  it('renders LoginSuccess entries with green badge', () => {
    render(<LoginHistoryTable entries={makeEntries([{ eventType: 'LoginSuccess' }])} />)
    expect(screen.getByText('LoginSuccess')).toBeInTheDocument()
  })

  it('renders LoginFailed entries', () => {
    render(<LoginHistoryTable entries={makeEntries([{ eventType: 'LoginFailed' }])} />)
    expect(screen.getByText('LoginFailed')).toBeInTheDocument()
  })

  it('renders IP address for each entry', () => {
    render(<LoginHistoryTable entries={makeEntries([{ ipAddress: '192.168.1.42' }])} />)
    expect(screen.getByText('192.168.1.42')).toBeInTheDocument()
  })

  it('shows empty state message when no entries', () => {
    render(<LoginHistoryTable entries={[]} />)
    expect(screen.getByText(/no login history/i)).toBeInTheDocument()
  })
})
