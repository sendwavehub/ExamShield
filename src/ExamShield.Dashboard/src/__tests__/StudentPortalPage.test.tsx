import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import StudentPortalPage from '../pages/StudentPortalPage'
import * as apiClient from '../api/client'

vi.mock('../api/client', () => ({
  api: {
    getStudentResults: vi.fn(),
  },
}))

const STUDENT_ID = 'stu-abc-123'

const mockResults = {
  studentId: STUDENT_ID,
  results: [
    {
      scoreId: 's1',
      captureId: 'c1',
      examId: 'e1',
      examName: 'Mathematics Final',
      correctAnswers: 45,
      totalQuestions: 50,
      percentage: 90.0,
      scoredAt: '2026-06-27T10:00:00Z',
      hashHex: 'abc123def456',
      isVerified: true,
    },
    {
      scoreId: 's2',
      captureId: 'c2',
      examId: 'e2',
      examName: 'Physics Midterm',
      correctAnswers: 30,
      totalQuestions: 50,
      percentage: 60.0,
      scoredAt: '2026-06-27T11:00:00Z',
      hashHex: 'def789abc012',
      isVerified: false,
    },
  ],
}

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <StudentPortalPage />
    </QueryClientProvider>
  )
}

beforeEach(() => {
  vi.mocked(apiClient.api.getStudentResults).mockResolvedValue(mockResults)
})

describe('StudentPortalPage', () => {
  it('renders page heading', () => {
    renderPage()
    expect(screen.getByRole('heading', { name: /student portal/i })).toBeInTheDocument()
  })

  it('shows student ID search field', () => {
    renderPage()
    expect(screen.getByPlaceholderText(/student id/i)).toBeInTheDocument()
  })

  it('shows lookup button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /look up/i })).toBeInTheDocument()
  })

  it('calls getStudentResults with entered ID', async () => {
    renderPage()
    fireEvent.change(screen.getByPlaceholderText(/student id/i), {
      target: { value: STUDENT_ID },
    })
    fireEvent.click(screen.getByRole('button', { name: /look up/i }))
    await waitFor(() =>
      expect(apiClient.api.getStudentResults).toHaveBeenCalledWith(STUDENT_ID)
    )
  })

  it('displays exam names after lookup', async () => {
    renderPage()
    fireEvent.change(screen.getByPlaceholderText(/student id/i), {
      target: { value: STUDENT_ID },
    })
    fireEvent.click(screen.getByRole('button', { name: /look up/i }))
    expect(await screen.findByText('Mathematics Final')).toBeInTheDocument()
    expect(screen.getByText('Physics Midterm')).toBeInTheDocument()
  })

  it('shows score percentages', async () => {
    renderPage()
    fireEvent.change(screen.getByPlaceholderText(/student id/i), {
      target: { value: STUDENT_ID },
    })
    fireEvent.click(screen.getByRole('button', { name: /look up/i }))
    expect(await screen.findByText(/90\.0%/)).toBeInTheDocument()
    expect(screen.getByText(/60\.0%/)).toBeInTheDocument()
  })

  it('shows verified badge for verified captures', async () => {
    renderPage()
    fireEvent.change(screen.getByPlaceholderText(/student id/i), {
      target: { value: STUDENT_ID },
    })
    fireEvent.click(screen.getByRole('button', { name: /look up/i }))
    await screen.findByText('Mathematics Final')
    const verifiedEls = screen.getAllByText(/verified/i)
    expect(verifiedEls.length).toBeGreaterThanOrEqual(1)
  })

  it('shows print button after results load', async () => {
    renderPage()
    fireEvent.change(screen.getByPlaceholderText(/student id/i), {
      target: { value: STUDENT_ID },
    })
    fireEvent.click(screen.getByRole('button', { name: /look up/i }))
    expect(await screen.findByRole('button', { name: /print/i })).toBeInTheDocument()
  })
})
