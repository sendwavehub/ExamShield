import { useEffect, useState } from 'react'

export type Theme = 'dark' | 'light' | 'white'

const THEME_CLASSES: Theme[] = ['dark', 'light', 'white']

function applyTheme(theme: Theme) {
  const root = document.documentElement
  root.classList.remove('light', 'white')
  if (theme !== 'dark') root.classList.add(theme)
}

export function useTheme() {
  const [theme, setThemeState] = useState<Theme>(() => {
    const saved = localStorage.getItem('examshield-theme') as Theme | null
    return saved && THEME_CLASSES.includes(saved) ? saved : 'dark'
  })

  useEffect(() => {
    applyTheme(theme)
    localStorage.setItem('examshield-theme', theme)
  }, [theme])

  function cycleTheme() {
    setThemeState(current => {
      const next = THEME_CLASSES[(THEME_CLASSES.indexOf(current) + 1) % THEME_CLASSES.length]
      return next
    })
  }

  function setTheme(theme: Theme) {
    setThemeState(theme)
  }

  return { theme, setTheme, cycleTheme }
}
