import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, beforeEach } from 'vitest'
import { useTheme } from '../hooks/useTheme'

describe('useTheme', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('light', 'white')
  })

  it('defaults to dark when no saved theme', () => {
    const { result } = renderHook(() => useTheme())
    expect(result.current.theme).toBe('dark')
  })

  it('reads saved theme from localStorage on init', () => {
    localStorage.setItem('examshield-theme', 'light')
    const { result } = renderHook(() => useTheme())
    expect(result.current.theme).toBe('light')
  })

  it('reads white theme from localStorage on init', () => {
    localStorage.setItem('examshield-theme', 'white')
    const { result } = renderHook(() => useTheme())
    expect(result.current.theme).toBe('white')
  })

  it('falls back to dark for unknown saved value', () => {
    localStorage.setItem('examshield-theme', 'neon')
    const { result } = renderHook(() => useTheme())
    expect(result.current.theme).toBe('dark')
  })

  it('setTheme changes the theme', () => {
    const { result } = renderHook(() => useTheme())
    act(() => { result.current.setTheme('light') })
    expect(result.current.theme).toBe('light')
  })

  it('setTheme persists to localStorage', () => {
    const { result } = renderHook(() => useTheme())
    act(() => { result.current.setTheme('white') })
    expect(localStorage.getItem('examshield-theme')).toBe('white')
  })

  it('cycleTheme cycles dark → light → white → dark', () => {
    const { result } = renderHook(() => useTheme())

    act(() => { result.current.cycleTheme() })
    expect(result.current.theme).toBe('light')

    act(() => { result.current.cycleTheme() })
    expect(result.current.theme).toBe('white')

    act(() => { result.current.cycleTheme() })
    expect(result.current.theme).toBe('dark')
  })

  it('cycleTheme persists each cycle step to localStorage', () => {
    const { result } = renderHook(() => useTheme())

    act(() => { result.current.cycleTheme() })
    expect(localStorage.getItem('examshield-theme')).toBe('light')

    act(() => { result.current.cycleTheme() })
    expect(localStorage.getItem('examshield-theme')).toBe('white')
  })
})
