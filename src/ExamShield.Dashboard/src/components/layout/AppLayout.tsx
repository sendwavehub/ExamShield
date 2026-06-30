import { useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import {
  LayoutDashboard, FileText, Shield, Monitor, ChevronLeft, Bell, User, Menu,
  ClipboardList, ScanLine, Eye, Star, BarChart2, Users, Settings, Search, ShieldCheck,
  BookOpen, KeyRound, FileImage, Trophy, MonitorSmartphone, MessageSquareWarning,
  Zap, PlayCircle, Moon, Sun, Sparkles,
} from 'lucide-react'
import { cn, getInitials } from '../../lib/utils'
import { useNotifications } from '../../hooks/useNotifications'
import { useTheme, type Theme } from '../../hooks/useTheme'
import NotificationPanel from '../ui/NotificationPanel'
import UserPanel from '../ui/UserPanel'

const THEME_ICONS: Record<Theme, typeof Moon> = { dark: Moon, light: Sun, white: Sparkles }
const THEME_LABELS: Record<Theme, string> = { dark: 'Dark', light: 'Light', white: 'White' }

const NAV_GROUPS = [
  {
    label: 'Core',
    items: [
      { label: 'Dashboard',         href: '/',              icon: LayoutDashboard  },
      { label: 'Examinations',      href: '/exams',         icon: ClipboardList    },
      { label: 'Capture Sessions',  href: '/captures',      icon: Monitor          },
      { label: 'Answer Sheets',     href: '/answer-sheets', icon: FileImage        },
    ],
  },
  {
    label: 'Processing',
    items: [
      { label: 'OCR Queue',         href: '/ocr-queue',     icon: ScanLine         },
      { label: 'Manual Review',     href: '/review',        icon: Eye              },
      { label: 'Review Requests',   href: '/review-requests', icon: MessageSquareWarning },
      { label: 'Scoring',           href: '/scoring',       icon: Star             },
      { label: 'Rankings',          href: '/rankings',      icon: Trophy           },
      { label: 'Results',           href: '/results',       icon: BarChart2        },
    ],
  },
  {
    label: 'Security',
    items: [
      { label: 'Audit Logs',        href: '/audit',         icon: FileText         },
      { label: 'Security Center',   href: '/security',      icon: Shield           },
      { label: 'Device Mgmt',       href: '/devices',       icon: Monitor          },
      { label: 'Sessions',          href: '/sessions',      icon: MonitorSmartphone },
    ],
  },
  {
    label: 'Admin',
    items: [
      { label: 'Users',             href: '/users',         icon: Users            },
      { label: 'Roles & Perms',     href: '/roles',         icon: ShieldCheck      },
      { label: 'Student Portal',    href: '/student',       icon: Users            },
      { label: 'Reports',           href: '/reports',       icon: BookOpen         },
      { label: 'MFA',               href: '/mfa',           icon: KeyRound         },
      { label: 'Settings',          href: '/settings',      icon: Settings         },
      { label: 'Public Verify',     href: '/public/verify', icon: Search           },
    ],
  },
  {
    label: 'Showcase',
    items: [
      { label: 'Pipeline Demo',     href: '/pipeline',      icon: PlayCircle       },
    ],
  },
]

interface AppLayoutProps {
  userName: string
  userEmail?: string | null
  hasMfa?: boolean
  expiresAt?: Date | null
  onLogout: () => void
  children: React.ReactNode
}

export default function AppLayout({ userName, userEmail, hasMfa, expiresAt, onLogout, children }: AppLayoutProps) {
  const [collapsed, setCollapsed] = useState(false)
  const [notifOpen, setNotifOpen] = useState(false)
  const [userPanelOpen, setUserPanelOpen] = useState(false)
  const bellRef = useRef<HTMLButtonElement>(null)
  const userRef = useRef<HTMLButtonElement>(null)
  const location = useLocation()
  const { notifications, dismiss, clearAll } = useNotifications()
  const { theme, cycleTheme } = useTheme()
  const ThemeIcon = THEME_ICONS[theme]

  const isActive = (href: string) =>
    href === '/' ? location.pathname === '/' : location.pathname.startsWith(href)

  return (
    <div className="flex h-screen aurora-bg text-foreground overflow-hidden">

      {/* ── Sidebar ─────────────────────────────────────────── */}
      <nav
        aria-label="Sidebar"
        className={cn(
          'flex flex-col glass-lg shrink-0 transition-all duration-250 overflow-hidden z-20 m-3 rounded-3xl',
          collapsed ? 'collapsed w-[68px]' : 'w-[220px]'
        )}
      >
        {/* Logo */}
        <div className={cn(
          'flex h-16 items-center shrink-0 px-4',
          collapsed ? 'justify-center' : 'justify-between'
        )}>
          {!collapsed && (
            <div className="flex items-center gap-2.5">
              <div className="flex h-7 w-7 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-[#78A6FF] shadow-glow-sm">
                <Zap className="h-3.5 w-3.5 text-white" />
              </div>
              <span className="text-sm font-bold tracking-tight text-gradient">ExamShield</span>
            </div>
          )}
          {collapsed && (
            <div className="flex h-7 w-7 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-[#78A6FF] shadow-glow-sm">
              <Zap className="h-3.5 w-3.5 text-white" />
            </div>
          )}
          <button
            aria-label="Toggle sidebar"
            onClick={() => setCollapsed(c => !c)}
            className="rounded-lg p-1 text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors"
          >
            {collapsed ? <Menu className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
          </button>
        </div>

        {/* Divider */}
        <div className="glass-divider mx-3" />

        {/* Nav groups */}
        <div className="flex-1 overflow-y-auto overflow-x-hidden py-3 px-2 space-y-4">
          {NAV_GROUPS.map(group => (
            <div key={group.label}>
              {!collapsed && (
                <p className="px-3 pb-1 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">
                  {group.label}
                </p>
              )}
              <ul className="space-y-0.5">
                {group.items.map(item => (
                  <li key={item.href}>
                    <Link
                      to={item.href}
                      aria-label={item.label}
                      title={collapsed ? item.label : undefined}
                      className={cn(
                        'nav-item',
                        collapsed && 'justify-center px-0 py-2.5',
                        isActive(item.href) && 'active'
                      )}
                    >
                      <item.icon className="h-[18px] w-[18px] shrink-0 stroke-[1.75]" />
                      {!collapsed && (
                        <span className="truncate">{item.label}</span>
                      )}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Bottom user hint */}
        {!collapsed && (
          <div className="glass-divider mx-3" />
        )}
        {!collapsed && (
          <div className="p-3">
            <div className="flex items-center gap-2.5 rounded-xl px-2 py-2">
              <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-primary/30 to-[#78A6FF]/30 ring-1 ring-white/10">
                <User className="h-3.5 w-3.5 text-primary" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate text-xs font-semibold text-foreground">{userName}</p>
                <p className="text-[10px] text-muted-foreground">Signed in</p>
              </div>
            </div>
          </div>
        )}
      </nav>

      {/* ── Main area ───────────────────────────────────────── */}
      <div className="flex flex-1 flex-col overflow-hidden min-w-0">

        {/* Top nav */}
        <header
          role="banner"
          className="flex h-16 shrink-0 items-center justify-between glass m-3 mb-0 rounded-3xl px-6"
        >
          <div className="flex items-center gap-3">
            <span className="text-sm font-semibold text-foreground">Admin Portal</span>
            <span className="hidden rounded-full bg-primary/15 px-2.5 py-0.5 text-[11px] font-semibold text-primary sm:inline-flex">
              ExamShield
            </span>
          </div>

          <div className="flex items-center gap-3">
            {/* Theme toggle */}
            <button
              aria-label={`Switch theme, current: ${THEME_LABELS[theme]}`}
              onClick={cycleTheme}
              title={`Theme: ${THEME_LABELS[theme]}`}
              className="flex h-9 w-9 items-center justify-center rounded-xl text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors"
            >
              <ThemeIcon className="h-4 w-4 stroke-[1.75]" />
            </button>

            {/* Notification bell */}
            <div className="relative">
              <button
                ref={bellRef}
                aria-label="Notifications"
                aria-expanded={notifOpen}
                onClick={() => setNotifOpen(o => !o)}
                className={cn(
                  'relative flex h-9 w-9 items-center justify-center rounded-xl transition-colors',
                  notifOpen
                    ? 'bg-primary/15 text-primary'
                    : 'text-muted-foreground hover:bg-muted/50 hover:text-foreground'
                )}
              >
                <Bell className="h-4 w-4 stroke-[1.75]" />
                {notifications.length > 0 && (
                  <span className="absolute right-1.5 top-1.5 flex h-2 w-2 items-center justify-center rounded-full bg-primary ring-2 ring-background">
                    <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-60" />
                  </span>
                )}
              </button>
              <NotificationPanel
                open={notifOpen}
                anchorRef={bellRef}
                notifications={notifications}
                onDismiss={dismiss}
                onClearAll={clearAll}
                onClose={() => setNotifOpen(false)}
              />
            </div>

            {/* Divider */}
            <div className="h-5 w-px bg-border" />

            {/* User */}
            <div className="relative">
              <button
                ref={userRef}
                aria-label="User menu"
                aria-expanded={userPanelOpen}
                onClick={() => setUserPanelOpen(o => !o)}
                className={cn(
                  'flex items-center gap-2.5 rounded-xl px-2 py-1.5 transition-colors',
                  userPanelOpen ? 'bg-primary/15' : 'hover:bg-muted/50'
                )}
              >
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-primary/30 to-[#78A6FF]/30 ring-1 ring-white/10 text-xs font-bold text-primary select-none">
                  {getInitials(userName)}
                </div>
                <span className="hidden text-sm font-medium text-foreground sm:block">{userName}</span>
              </button>
              <UserPanel
                open={userPanelOpen}
                anchorRef={userRef}
                userName={userName}
                userEmail={userEmail}
                hasMfa={hasMfa}
                expiresAt={expiresAt}
                onLogout={onLogout}
                onClose={() => setUserPanelOpen(false)}
              />
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-auto p-3 pt-3">
          <div className="animate-in h-full">
            {children}
          </div>
        </main>
      </div>
    </div>
  )
}
