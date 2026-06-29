import { useEffect, useRef } from 'react'
import { createPortal } from 'react-dom'
import { useNavigate } from 'react-router-dom'
import {
  User, KeyRound, Shield, MonitorSmartphone, LogOut,
  CheckCircle2, AlertTriangle, Clock,
} from 'lucide-react'
import { cn, getInitials } from '../../lib/utils'

const ROLE_COLORS: Record<string, string> = {
  'super administrator':   'bg-red-500/15 text-red-400 ring-red-500/20',
  administrator:           'bg-red-500/15 text-red-400 ring-red-500/20',
  'security administrator':'bg-orange-500/15 text-orange-400 ring-orange-500/20',
  'system administrator':  'bg-amber-500/15 text-amber-400 ring-amber-500/20',
  'exam manager':          'bg-purple-500/15 text-purple-400 ring-purple-500/20',
  'device manager':        'bg-indigo-500/15 text-indigo-400 ring-indigo-500/20',
  invigilator:             'bg-blue-500/15 text-blue-400 ring-blue-500/20',
  operator:                'bg-blue-500/15 text-blue-400 ring-blue-500/20',
  'manual reviewer':       'bg-teal-500/15 text-teal-400 ring-teal-500/20',
  'review supervisor':     'bg-green-500/15 text-green-400 ring-green-500/20',
  'result publisher':      'bg-emerald-500/15 text-emerald-400 ring-emerald-500/20',
  auditor:                 'bg-slate-500/15 text-slate-400 ring-slate-500/20',
  'investigation officer': 'bg-yellow-500/15 text-yellow-400 ring-yellow-500/20',
  student:                 'bg-sky-500/15 text-sky-400 ring-sky-500/20',
}

function formatExpiry(expiresAt: Date | null | undefined): string {
  if (!expiresAt) return 'Unknown'
  const diff = expiresAt.getTime() - Date.now()
  if (diff <= 0) return 'Expired'
  const min = Math.floor(diff / 60000)
  if (min < 60) return `${min} min`
  const h = Math.floor(min / 60)
  return `${h}h ${min % 60}m`
}

const ACCOUNT_LINKS = [
  { label: 'My Profile',      icon: User,              href: '/profile'         },
  { label: 'Change Password', icon: KeyRound,          href: '/change-password' },
  { label: 'MFA Settings',    icon: Shield,            href: '/mfa'             },
]

const SECURITY_LINKS = [
  { label: 'Active Sessions', icon: MonitorSmartphone, href: '/sessions'        },
]

interface Props {
  open: boolean
  anchorRef: React.RefObject<HTMLButtonElement | null>
  userName: string
  userEmail?: string | null
  hasMfa?: boolean
  expiresAt?: Date | null
  onLogout: () => void
  onClose: () => void
}

export default function UserPanel({
  open, anchorRef, userName, userEmail, hasMfa, expiresAt, onLogout, onClose,
}: Props) {
  const panelRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()

  useEffect(() => {
    if (!open) return
    function handleClick(e: MouseEvent) {
      const target = e.target as Node
      if (
        panelRef.current && !panelRef.current.contains(target) &&
        anchorRef.current && !anchorRef.current.contains(target)
      ) {
        onClose()
      }
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [open, onClose, anchorRef])

  if (!open) return null

  const anchor = anchorRef.current?.getBoundingClientRect()
  const top = anchor ? anchor.bottom + 8 : 80
  const right = anchor ? window.innerWidth - anchor.right : 16

  const roleColor = ROLE_COLORS[userName.toLowerCase()] ?? 'bg-primary/15 text-primary ring-primary/20'
  const expiry = formatExpiry(expiresAt)

  function navTo(href: string) {
    navigate(href)
    onClose()
  }

  function handleLogout() {
    onClose()
    onLogout()
  }

  return createPortal(
    <div
      ref={panelRef}
      role="dialog"
      aria-label="User menu"
      style={{
        position: 'fixed',
        top,
        right,
        width: 320,
        zIndex: 9999,
        background: 'hsl(220 35% 13%)',
        border: '1px solid rgba(255,255,255,0.10)',
        borderRadius: '1rem',
        boxShadow: '0 20px 60px rgba(0,0,0,0.70)',
        overflow: 'hidden',
      }}
    >
      {/* Identity header */}
      <div
        className="px-5 py-4"
        style={{ background: 'hsl(220 35% 11%)', borderBottom: '1px solid rgba(255,255,255,0.08)' }}
      >
        <div className="flex items-center gap-3 mb-3">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-primary/40 to-[#78A6FF]/40 ring-1 ring-white/10 text-primary font-bold text-sm select-none">
            {getInitials(userName)}
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-sm font-semibold text-foreground truncate">{userName}</p>
            {userEmail && (
              <p className="text-[11px] text-muted-foreground truncate mt-0.5">{userEmail}</p>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <span className={cn('rounded-md px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide ring-1', roleColor)}>
            {userName}
          </span>
          {hasMfa ? (
            <span className="flex items-center gap-1 rounded-md bg-green-500/10 px-2 py-0.5 text-[10px] font-semibold text-green-400 ring-1 ring-green-500/20">
              <CheckCircle2 className="h-2.5 w-2.5" />
              MFA Active
            </span>
          ) : (
            <span className="flex items-center gap-1 rounded-md bg-yellow-500/10 px-2 py-0.5 text-[10px] font-semibold text-yellow-400 ring-1 ring-yellow-500/20">
              <AlertTriangle className="h-2.5 w-2.5" />
              MFA Off
            </span>
          )}
        </div>
      </div>

      {/* Session expiry row */}
      <div
        className="flex items-center gap-2 px-5 py-2.5"
        style={{ borderBottom: '1px solid rgba(255,255,255,0.06)', background: 'hsl(220 35% 12%)' }}
      >
        <Clock className="h-3 w-3 text-muted-foreground shrink-0" />
        <span className="text-[11px] text-muted-foreground">Session expires in</span>
        <span className="ml-auto text-[11px] font-semibold text-foreground tabular-nums">{expiry}</span>
      </div>

      {/* Account section */}
      <div className="py-1.5">
        <p className="px-5 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/50">
          Account
        </p>
        {ACCOUNT_LINKS.map(item => (
          <button
            key={item.href}
            onClick={() => navTo(item.href)}
            className="flex w-full items-center gap-3 px-5 py-2.5 text-sm text-muted-foreground hover:bg-white/5 hover:text-foreground transition-colors"
          >
            <item.icon className="h-4 w-4 shrink-0 stroke-[1.75]" />
            {item.label}
          </button>
        ))}
      </div>

      {/* Security section */}
      <div
        className="py-1.5"
        style={{ borderTop: '1px solid rgba(255,255,255,0.06)' }}
      >
        <p className="px-5 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/50">
          Security
        </p>
        {SECURITY_LINKS.map(item => (
          <button
            key={item.href}
            onClick={() => navTo(item.href)}
            className="flex w-full items-center gap-3 px-5 py-2.5 text-sm text-muted-foreground hover:bg-white/5 hover:text-foreground transition-colors"
          >
            <item.icon className="h-4 w-4 shrink-0 stroke-[1.75]" />
            {item.label}
          </button>
        ))}
      </div>

      {/* Sign out */}
      <div
        className="p-3"
        style={{ borderTop: '1px solid rgba(255,255,255,0.08)' }}
      >
        <button
          onClick={handleLogout}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-red-500/10 px-4 py-2.5 text-sm font-medium text-red-400 hover:bg-red-500/20 transition-colors ring-1 ring-red-500/20"
        >
          <LogOut className="h-4 w-4 stroke-[1.75]" />
          Sign Out
        </button>
      </div>
    </div>,
    document.body
  )
}
