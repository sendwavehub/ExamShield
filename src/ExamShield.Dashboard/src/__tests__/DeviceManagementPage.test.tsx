import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DeviceManagementPage from '../pages/DeviceManagementPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getDevices: vi.fn(),
    approveDevice: vi.fn(),
    disableDevice: vi.fn(),
    enableDevice: vi.fn(),
    blacklistDevice: vi.fn(),
    deviceHeartbeat: vi.fn(),
  },
}))

const baseDevice = {
  registeredAt: '2026-06-01T08:00:00Z',
  lastSeenAt: null,
  blacklistReason: null,
}

const mockDevices = [
  { ...baseDevice, deviceId: 'dev-1', name: 'Scanner-01', status: 'Approved',    isActive: true },
  { ...baseDevice, deviceId: 'dev-2', name: 'Scanner-02', status: 'Disabled',    isActive: false },
  { ...baseDevice, deviceId: 'dev-3', name: 'Scanner-03', status: 'Pending',     isActive: false },
  {
    ...baseDevice, deviceId: 'dev-4', name: 'Scanner-04', status: 'Blacklisted', isActive: false,
    blacklistReason: 'Suspected tampering',
  },
]

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <DeviceManagementPage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getDevices).mockResolvedValue({ devices: mockDevices })
  vi.mocked(apiClient.api.approveDevice).mockResolvedValue(undefined)
  vi.mocked(apiClient.api.disableDevice).mockResolvedValue(undefined)
  vi.mocked(apiClient.api.enableDevice).mockResolvedValue(undefined)
  vi.mocked(apiClient.api.blacklistDevice).mockResolvedValue(undefined)
  vi.mocked(apiClient.api.deviceHeartbeat).mockResolvedValue(undefined)
})

describe('DeviceManagementPage — display', () => {
  it('renders page heading', async () => {
    renderPage()
    expect(await screen.findByRole('heading', { name: /device management/i })).toBeInTheDocument()
  })

  it('renders a row for each device', async () => {
    renderPage()
    const rows = await screen.findAllByRole('row')
    expect(rows).toHaveLength(5) // header + 4 rows
  })

  it('displays all device names', async () => {
    renderPage()
    for (const name of ['Scanner-01', 'Scanner-02', 'Scanner-03', 'Scanner-04']) {
      expect(await screen.findByText(name)).toBeInTheDocument()
    }
  })

  it('shows device count', async () => {
    renderPage()
    expect(await screen.findByText('4 devices')).toBeInTheDocument()
  })

  it('shows status chip for each device', async () => {
    renderPage()
    await screen.findByText('Scanner-01')
    expect(screen.getByText('Approved')).toBeInTheDocument()
    expect(screen.getByText('Disabled')).toBeInTheDocument()
    expect(screen.getByText('Pending')).toBeInTheDocument()
    expect(screen.getByText('Blacklisted')).toBeInTheDocument()
  })

  it('shows blacklist reason for blacklisted device', async () => {
    renderPage()
    expect(await screen.findByText(/suspected tampering/i)).toBeInTheDocument()
  })

  it('shows loading state initially', () => {
    vi.mocked(apiClient.api.getDevices).mockImplementation(() => new Promise(() => {}))
    renderPage()
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('shows error state when fetch fails', async () => {
    vi.mocked(apiClient.api.getDevices).mockRejectedValue(new Error('Network error'))
    renderPage()
    expect(await screen.findByText(/failed to load devices/i)).toBeInTheDocument()
  })
})

describe('DeviceManagementPage — Approve action', () => {
  it('shows Approve button for Pending device', async () => {
    renderPage()
    await screen.findByText('Scanner-03')
    expect(screen.getByRole('button', { name: /approve/i })).toBeInTheDocument()
  })

  it('does not show Approve for Approved device', async () => {
    // Filter to only the approved device to avoid ambiguity
    vi.mocked(apiClient.api.getDevices).mockResolvedValue({
      devices: [{ ...baseDevice, deviceId: 'dev-1', name: 'Scanner-01', status: 'Approved', isActive: true }],
    })
    renderPage()
    await screen.findByText('Scanner-01')
    expect(screen.queryByRole('button', { name: /approve/i })).not.toBeInTheDocument()
  })

  it('calls approveDevice when Approve is clicked', async () => {
    renderPage()
    await screen.findByText('Scanner-03')
    fireEvent.click(screen.getByRole('button', { name: /approve/i }))
    await waitFor(() => expect(apiClient.api.approveDevice).toHaveBeenCalledWith('dev-3'))
  })
})

describe('DeviceManagementPage — Disable action', () => {
  it('shows Disable button for Approved device', async () => {
    renderPage()
    await screen.findByText('Scanner-01')
    expect(screen.getByRole('button', { name: /^disable$/i })).toBeInTheDocument()
  })

  it('calls disableDevice when Disable is clicked', async () => {
    renderPage()
    await screen.findByText('Scanner-01')
    fireEvent.click(screen.getByRole('button', { name: /^disable$/i }))
    await waitFor(() => expect(apiClient.api.disableDevice).toHaveBeenCalledWith('dev-1'))
  })
})

describe('DeviceManagementPage — Re-enable action', () => {
  it('shows Re-enable button for Disabled device', async () => {
    renderPage()
    await screen.findByText('Scanner-02')
    expect(screen.getByRole('button', { name: /re-enable/i })).toBeInTheDocument()
  })

  it('calls enableDevice when Re-enable is clicked', async () => {
    renderPage()
    await screen.findByText('Scanner-02')
    fireEvent.click(screen.getByRole('button', { name: /re-enable/i }))
    await waitFor(() => expect(apiClient.api.enableDevice).toHaveBeenCalledWith('dev-2'))
  })
})

describe('DeviceManagementPage — Blacklist action', () => {
  it('shows Blacklist button for non-blacklisted devices', async () => {
    renderPage()
    await screen.findByText('Scanner-01')
    const buttons = screen.getAllByRole('button', { name: /blacklist/i })
    expect(buttons.length).toBeGreaterThanOrEqual(1)
  })

  it('does not show Blacklist button for already-blacklisted device', async () => {
    vi.mocked(apiClient.api.getDevices).mockResolvedValue({
      devices: [{ ...baseDevice, deviceId: 'dev-4', name: 'Scanner-04', status: 'Blacklisted', isActive: false, blacklistReason: 'Bad actor' }],
    })
    renderPage()
    await screen.findByText('Scanner-04')
    expect(screen.queryByRole('button', { name: /^blacklist$/i })).not.toBeInTheDocument()
  })

  it('shows reason input when Blacklist button is clicked', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('Scanner-01')
    await user.click(screen.getAllByRole('button', { name: /blacklist/i })[0])
    expect(screen.getByPlaceholderText(/reason/i)).toBeInTheDocument()
  })

  it('Confirm button is disabled when reason is empty', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('Scanner-01')
    await user.click(screen.getAllByRole('button', { name: /blacklist/i })[0])
    expect(screen.getByRole('button', { name: /confirm/i })).toBeDisabled()
  })

  it('calls blacklistDevice with reason when Confirm is clicked', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('Scanner-01')
    await user.click(screen.getAllByRole('button', { name: /blacklist/i })[0])
    await user.type(screen.getByPlaceholderText(/reason/i), 'Stolen device')
    await user.click(screen.getByRole('button', { name: /confirm/i }))
    await waitFor(() =>
      expect(apiClient.api.blacklistDevice).toHaveBeenCalledWith('dev-1', 'Stolen device')
    )
  })

  it('cancel (✕) button hides the reason input', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('Scanner-01')
    await user.click(screen.getAllByRole('button', { name: /blacklist/i })[0])
    await user.click(screen.getByRole('button', { name: '✕' }))
    expect(screen.queryByPlaceholderText(/reason/i)).not.toBeInTheDocument()
  })
})

describe('DeviceManagementPage — Ping action', () => {
  it('shows Ping button for each device', async () => {
    renderPage()
    await screen.findByText('Scanner-01')
    const pings = screen.getAllByRole('button', { name: /ping/i })
    expect(pings.length).toBe(4)
  })

  it('Ping is disabled for inactive device', async () => {
    renderPage()
    await screen.findByText('Scanner-02')
    // dev-2 is inactive — find its Ping button by its position in the row
    // All inactive devices have disabled Ping
    const pings = screen.getAllByRole('button', { name: /ping/i })
    const disabledPings = pings.filter(b => b.hasAttribute('disabled'))
    expect(disabledPings.length).toBeGreaterThan(0)
  })

  it('calls deviceHeartbeat when Ping is clicked on active device', async () => {
    renderPage()
    await screen.findByText('Scanner-01')
    // dev-1 is the only active device (isActive: true)
    const activePing = screen.getAllByRole('button', { name: /ping/i }).find(b => !b.hasAttribute('disabled'))
    expect(activePing).toBeDefined()
    fireEvent.click(activePing!)
    await waitFor(() => expect(apiClient.api.deviceHeartbeat).toHaveBeenCalledWith('dev-1'))
  })
})
