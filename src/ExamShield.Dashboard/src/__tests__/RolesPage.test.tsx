import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import RolesPage from '../pages/RolesPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getRoles: vi.fn(),
  },
}))

const mockRoles = [
  {
    roleName: 'SuperAdministrator',
    displayName: 'Super Administrator',
    permissions: ['users.manage', 'roles.manage', 'audit.read', 'exams.read'],
  },
  {
    roleName: 'Invigilator',
    displayName: 'Invigilator',
    permissions: ['capture.write', 'capture.read'],
  },
  {
    roleName: 'Auditor',
    displayName: 'Auditor',
    permissions: ['audit.read', 'exams.read', 'capture.read'],
  },
]

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <RolesPage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getRoles).mockResolvedValue({ roles: mockRoles })
})

describe('RolesPage', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /roles/i })).toBeInTheDocument()
  })

  it('shows all roles', async () => {
    renderPage()
    await screen.findByText('3 roles')
    expect(screen.getAllByText('Super Administrator').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('Invigilator').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('Auditor').length).toBeGreaterThanOrEqual(1)
  })

  it('shows permission columns', async () => {
    renderPage()
    await screen.findByText('3 roles')
    expect(screen.getAllByText('users.manage').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('audit.read').length).toBeGreaterThanOrEqual(1)
  })

  it('renders checked indicator for granted permissions', async () => {
    renderPage()
    await screen.findByText('3 roles')
    const granted = document.querySelectorAll('[data-granted="true"]')
    expect(granted.length).toBeGreaterThan(0)
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.api.getRoles).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows role count badge', async () => {
    renderPage()
    expect(await screen.findByText(/3 roles/i)).toBeInTheDocument()
  })

  it('renders permission count per role', async () => {
    renderPage()
    await screen.findByText('3 roles')
    expect(screen.getByText('4 permissions')).toBeInTheDocument()
    expect(screen.getAllByText('2 permissions').length).toBeGreaterThanOrEqual(1)
  })
})
