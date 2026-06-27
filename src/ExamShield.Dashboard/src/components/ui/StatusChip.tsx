import { cn } from '../../lib/utils'

export type StatusVariant = 'success' | 'danger' | 'warning' | 'info' | 'muted'

interface StatusChipProps {
  label: string
  variant: StatusVariant
}

const variantClasses: Record<StatusVariant, string> = {
  success: 'text-green-500 bg-green-500/10',
  danger:  'text-red-500 bg-red-500/10',
  warning: 'text-yellow-500 bg-yellow-500/10',
  info:    'text-blue-500 bg-blue-500/10',
  muted:   'text-muted-foreground bg-muted',
}

export default function StatusChip({ label, variant }: StatusChipProps) {
  return (
    <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium', variantClasses[variant])}>
      {label}
    </span>
  )
}
