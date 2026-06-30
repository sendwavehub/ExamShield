import { useEffect, useRef } from 'react'
import { createPortal } from 'react-dom'
import { X, BellOff, AlertTriangle, AlertCircle, Info, Zap } from 'lucide-react'
import type { RealtimeNotification } from '../../hooks/useNotifications'
import { cn } from '../../lib/utils'

const SEVERITY_CONFIG = {
  Critical: { bar: 'bg-red-500',    badge: 'bg-red-500/15 text-red-400',    icon: AlertCircle,   label: 'Critical' },
  High:     { bar: 'bg-orange-500', badge: 'bg-orange-500/15 text-orange-400', icon: AlertTriangle, label: 'High'     },
  Warning:  { bar: 'bg-yellow-500', badge: 'bg-yellow-500/15 text-yellow-400', icon: Zap,           label: 'Warning'  },
  Info:     { bar: 'bg-blue-500',   badge: 'bg-blue-500/15 text-blue-400',   icon: Info,          label: 'Info'     },
} as const

type Severity = keyof typeof SEVERITY_CONFIG

interface Props {
  open: boolean
  anchorRef: React.RefObject<HTMLButtonElement | null>
  notifications: RealtimeNotification[]
  onDismiss: (index: number) => void
  onClearAll: () => void
  onClose: () => void
}

export default function NotificationPanel({ open, anchorRef, notifications, onDismiss, onClearAll, onClose }: Props) {
  const panelRef = useRef<HTMLDivElement>(null)

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

  return createPortal(
    <div
      ref={panelRef}
      role="dialog"
      aria-label="Notifications"
      style={{
        position: 'fixed',
        top,
        right,
        width: 380,
        maxHeight: 520,
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column',
        background: 'hsl(var(--card))',
        border: '1px solid hsl(var(--border))',
        borderRadius: '1rem',
        boxShadow: 'var(--glass-shadow-lg)',
        overflow: 'hidden',
      }}
    >
      {/* Header */}
      <div
        className="flex items-center justify-between px-4 py-3"
        style={{ background: 'hsl(var(--background))', borderBottom: '1px solid hsl(var(--border))' }}
      >
        <div className="flex items-center gap-2">
          <span className="text-sm font-semibold text-foreground">Notifications</span>
          {notifications.length > 0 && (
            <span className="rounded-full bg-primary/20 px-2 py-0.5 text-[11px] font-semibold text-primary">
              {notifications.length}
            </span>
          )}
        </div>
        <div className="flex items-center gap-1">
          {notifications.length > 0 && (
            <button
              onClick={onClearAll}
              className="rounded-lg px-2.5 py-1 text-[11px] font-medium text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors"
            >
              Clear all
            </button>
          )}
          <button
            onClick={onClose}
            aria-label="Close notifications"
            className="flex h-7 w-7 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* List */}
      <div className="flex-1 overflow-y-auto">
        {notifications.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-14 text-muted-foreground">
            <BellOff className="h-8 w-8 opacity-30" />
            <p className="text-sm">No notifications</p>
          </div>
        ) : (
          <ul>
            {notifications.map((n, i) => {
              const cfg = SEVERITY_CONFIG[n.severity as Severity] ?? SEVERITY_CONFIG.Info
              const Icon = cfg.icon
              return (
                <li
                  key={i}
                  className={cn(
                    'relative flex gap-3 px-4 py-3 hover:bg-muted/50 transition-colors',
                    i < notifications.length - 1 && 'border-b border-border/50'
                  )}
                >
                  <div className={cn('absolute left-0 top-0 bottom-0 w-0.5 rounded-r', cfg.bar)} />

                  <div className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-lg mt-0.5', cfg.badge)}>
                    <Icon className="h-3.5 w-3.5" />
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-0.5">
                      <span className={cn('rounded px-1.5 py-px text-[10px] font-semibold uppercase tracking-wide', cfg.badge)}>
                        {cfg.label}
                      </span>
                      <span className="text-[10px] text-muted-foreground">
                        {new Date(n.occurredAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                    <p className="text-xs font-medium text-foreground/80 mb-0.5">
                      {n.type.replace(/([A-Z])/g, ' $1').trim()}
                    </p>
                    <p className="text-xs text-muted-foreground break-words leading-relaxed">{n.message}</p>
                  </div>

                  <button
                    onClick={() => onDismiss(i)}
                    aria-label="Dismiss notification"
                    className="flex h-6 w-6 shrink-0 items-center justify-center rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors mt-0.5"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </div>,
    document.body
  )
}
