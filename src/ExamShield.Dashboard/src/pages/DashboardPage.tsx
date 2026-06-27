import { ShieldCheck, Clock, CheckCircle, AlertTriangle } from 'lucide-react'

export interface DashboardStats {
  totalCaptures: number
  pendingReview: number
  verifiedToday: number
  alertCount: number
}

interface DashboardPageProps {
  stats: DashboardStats
}

function StatCard({
  label,
  value,
  icon: Icon,
  accent,
}: {
  label: string
  value: string | number
  icon: React.ElementType
  accent?: string
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-muted-foreground">{label}</p>
        <Icon className={`h-5 w-5 ${accent ?? 'text-muted-foreground'}`} />
      </div>
      <p className="mt-3 text-3xl font-bold text-foreground">
        {typeof value === 'number' ? value.toLocaleString() : value}
      </p>
    </div>
  )
}

export default function DashboardPage({ stats }: DashboardPageProps) {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>
        {stats.alertCount > 0 && (
          <span className="flex items-center gap-1.5 rounded-full bg-red-500/10 px-3 py-1 text-sm font-medium text-red-500">
            <AlertTriangle className="h-4 w-4" />
            {stats.alertCount} alert{stats.alertCount !== 1 ? 's' : ''}
          </span>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Total Captures"
          value={stats.totalCaptures}
          icon={ShieldCheck}
          accent="text-cyan-500"
        />
        <StatCard
          label="Pending Review"
          value={stats.pendingReview}
          icon={Clock}
          accent="text-yellow-500"
        />
        <StatCard
          label="Verified Today"
          value={stats.verifiedToday}
          icon={CheckCircle}
          accent="text-green-500"
        />
        <StatCard
          label="Active Alerts"
          value={stats.alertCount}
          icon={AlertTriangle}
          accent={stats.alertCount > 0 ? 'text-red-500' : 'text-muted-foreground'}
        />
      </div>
    </div>
  )
}
