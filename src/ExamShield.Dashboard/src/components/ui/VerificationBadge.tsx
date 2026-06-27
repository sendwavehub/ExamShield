import { CheckCircle, AlertTriangle, Clock } from 'lucide-react'
import { cn } from '../../lib/utils'

export type VerificationStatus = 'valid' | 'invalid' | 'pending'

interface VerificationBadgeProps {
  status: VerificationStatus
}

const config: Record<VerificationStatus, { label: string; icon: React.ElementType; classes: string }> = {
  valid:   { label: 'Verified', icon: CheckCircle,    classes: 'text-green-500 bg-green-500/10' },
  invalid: { label: 'Tampered', icon: AlertTriangle,  classes: 'text-red-500 bg-red-500/10' },
  pending: { label: 'Pending',  icon: Clock,          classes: 'text-yellow-500 bg-yellow-500/10' },
}

export default function VerificationBadge({ status }: VerificationBadgeProps) {
  const { label, icon: Icon, classes } = config[status]
  return (
    <span
      data-testid="verification-badge"
      className={cn('inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium', classes)}
    >
      <Icon className="h-3 w-3" />
      {label}
    </span>
  )
}
