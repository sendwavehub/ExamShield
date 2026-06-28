import { useQueryClient, useQuery } from '@tanstack/react-query'
import {
  ShieldCheck, Clock, CheckCircle, AlertTriangle,
  BookOpen, BarChart2, Cpu, Layers, RefreshCw,
  TrendingUp, Users, Activity,
} from 'lucide-react'
import {
  PieChart, Pie, Cell, Tooltip, ResponsiveContainer,
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Legend,
  RadarChart, Radar, PolarGrid, PolarAngleAxis,
} from 'recharts'
import { api } from '../api/client'
import { useDashboardStats } from '../hooks/useDashboardStats'

// ── Colour palette (dark-mode safe) ────────────────────────────────────────
const C = {
  cyan:   '#22d3ee',
  yellow: '#facc15',
  green:  '#4ade80',
  red:    '#f87171',
  blue:   '#60a5fa',
  violet: '#a78bfa',
  slate:  '#94a3b8',
  orange: '#fb923c',
}

const TOOLTIP_STYLE = {
  backgroundColor: 'hsl(222 47% 14%)',
  border: '1px solid hsl(222 47% 22%)',
  borderRadius: 8,
  color: 'hsl(210 40% 98%)',
  fontSize: 12,
}

// ── Sub-components ──────────────────────────────────────────────────────────
function StatCard({
  label, value, sub, icon: Icon, accent, loading = false,
}: {
  label: string; value: string | number; sub?: string
  icon: React.ElementType; accent: string; loading?: boolean
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-5 shadow-sm flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{label}</p>
        <span className={`rounded-md p-1.5 ${accent.replace('text-', 'bg-').replace('-400', '-500/10').replace('-500', '-500/10')}`}>
          <Icon className={`h-4 w-4 ${accent}`} />
        </span>
      </div>
      {loading ? (
        <div className="h-8 w-20 animate-pulse rounded bg-muted" />
      ) : (
        <p className={`text-3xl font-bold tabular-nums ${accent}`}>
          {typeof value === 'number' ? value.toLocaleString() : value}
        </p>
      )}
      {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
    </div>
  )
}

function ChartCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
      <p className="mb-4 text-sm font-semibold text-foreground">{title}</p>
      {children}
    </div>
  )
}

function SeverityBadge({ severity }: { severity: string }) {
  const map: Record<string, string> = {
    Critical: 'bg-red-500/15 text-red-400',
    High:     'bg-orange-500/15 text-orange-400',
    Warning:  'bg-yellow-500/15 text-yellow-400',
    Info:     'bg-cyan-500/15 text-cyan-400',
  }
  return (
    <span className={`rounded px-1.5 py-0.5 text-[10px] font-bold uppercase ${map[severity] ?? 'bg-muted text-muted-foreground'}`}>
      {severity}
    </span>
  )
}

function ActionIcon({ action }: { action: string }) {
  const iconMap: Record<string, { icon: React.ElementType; color: string }> = {
    HashVerified:         { icon: ShieldCheck, color: 'text-cyan-400' },
    ScoreGenerated:       { icon: BarChart2,   color: 'text-violet-400' },
    ResultPublished:      { icon: TrendingUp,  color: 'text-green-400' },
    ManualReviewCompleted:{ icon: CheckCircle, color: 'text-green-400' },
    ManualReviewStarted:  { icon: Clock,       color: 'text-yellow-400' },
    OCRCompleted:         { icon: Cpu,         color: 'text-blue-400' },
    ImageUploaded:        { icon: Layers,      color: 'text-blue-400' },
    CaptureRegistered:    { icon: ShieldCheck, color: 'text-slate-400' },
    DeviceRegistered:     { icon: Activity,    color: 'text-slate-400' },
    UserCreated:          { icon: Users,       color: 'text-slate-400' },
  }
  const { icon: Icon, color } = iconMap[action] ?? { icon: Activity, color: 'text-muted-foreground' }
  return <Icon className={`h-3.5 w-3.5 shrink-0 ${color}`} />
}

// ── Main page ───────────────────────────────────────────────────────────────
export default function DashboardPage() {
  const qc = useQueryClient()

  const { data: stats, isLoading: statsLoading, dataUpdatedAt } = useDashboardStats()

  const { data: statistics } = useQuery({
    queryKey: ['statistics'],
    queryFn: api.getStatistics,
    refetchInterval: 60_000,
  })

  const { data: capturesData } = useQuery({
    queryKey: ['captures-dash'],
    queryFn: () => api.getCaptures(1, 200),
    refetchInterval: 30_000,
  })

  const { data: examsData } = useQuery({
    queryKey: ['exams-dash'],
    queryFn: () => api.getExams(1, 100),
    refetchInterval: 60_000,
  })

  const { data: devicesData } = useQuery({
    queryKey: ['devices-dash'],
    queryFn: api.getDevices,
    refetchInterval: 60_000,
  })

  const { data: securityData } = useQuery({
    queryKey: ['security-dash'],
    queryFn: () => api.getSecurityEvents(50),
    refetchInterval: 30_000,
  })

  const { data: auditData } = useQuery({
    queryKey: ['audit-dash'],
    queryFn: () => api.getAuditLog({ pageSize: 8 }),
    refetchInterval: 30_000,
  })

  const { data: resultsData } = useQuery({
    queryKey: ['results-dash'],
    queryFn: () => api.getResults(),
    refetchInterval: 60_000,
  })

  // ── Derived data ──────────────────────────────────────────────────────────
  const captures = capturesData?.captures ?? []
  const exams    = examsData?.exams ?? []
  const devices  = devicesData?.devices ?? []
  const events   = securityData?.events ?? []
  const audit    = auditData?.entries ?? []
  const scores   = resultsData?.results ?? []

  // Capture status donut
  const captureStatusData = ['Created','Uploaded','Verified','Tampered'].map(status => ({
    name: status,
    value: captures.filter(c => c.status === status).length,
  })).filter(d => d.value > 0)

  const captureColors: Record<string, string> = {
    Created: C.slate, Uploaded: C.blue, Verified: C.cyan, Tampered: C.red,
  }

  // Security event severity bar
  const severityOrder = ['Critical', 'High', 'Warning', 'Info']
  const securityBarData = severityOrder.map(sev => ({
    severity: sev,
    count: events.filter(e => e.severity === sev).length,
  }))
  const severityColors: Record<string, string> = {
    Critical: C.red, High: C.orange, Warning: C.yellow, Info: C.cyan,
  }

  // Exam status breakdown
  const examStatusData = ['Active','Closed','Draft'].map(status => ({
    status,
    count: exams.filter(e => e.status === status).length,
  }))
  const examStatusColors: Record<string, string> = {
    Active: C.cyan, Closed: C.violet, Draft: C.slate,
  }

  // Score distribution radar (by score bucket)
  const scoreBuckets = [
    { range: '90–100%', count: scores.filter(s => s.percentage >= 90).length },
    { range: '75–89%',  count: scores.filter(s => s.percentage >= 75 && s.percentage < 90).length },
    { range: '60–74%',  count: scores.filter(s => s.percentage >= 60 && s.percentage < 75).length },
    { range: '<60%',    count: scores.filter(s => s.percentage < 60).length },
  ]

  // Score bar data — individual scores sorted desc
  const scoreBarData = [...scores]
    .sort((a, b) => b.percentage - a.percentage)
    .slice(0, 10)
    .map((s, i) => ({ label: `#${i + 1}`, pct: Math.round(s.percentage) }))

  // Secondary stats
  const activeDevices  = devices.filter(d => d.isActive).length
  const pendingDevices = devices.filter(d => d.status === 'Pending').length
  const activeExams    = exams.filter(e => e.status === 'Active').length
  const updatedAt = dataUpdatedAt ? new Date(dataUpdatedAt).toLocaleTimeString() : '—'

  const refreshAll = () => {
    qc.invalidateQueries({ queryKey: ['dashboard-stats'] })
    qc.invalidateQueries({ queryKey: ['statistics'] })
    qc.invalidateQueries({ queryKey: ['captures-dash'] })
    qc.invalidateQueries({ queryKey: ['exams-dash'] })
    qc.invalidateQueries({ queryKey: ['devices-dash'] })
    qc.invalidateQueries({ queryKey: ['security-dash'] })
    qc.invalidateQueries({ queryKey: ['audit-dash'] })
    qc.invalidateQueries({ queryKey: ['results-dash'] })
  }

  // ── Render ────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-6">

      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>
          <p className="text-xs text-muted-foreground mt-0.5">Last updated: {updatedAt}</p>
        </div>
        <div className="flex items-center gap-3">
          {(stats?.activeAlerts ?? 0) > 0 && (
            <span className="flex items-center gap-1.5 rounded-full bg-red-500/10 px-3 py-1 text-xs font-semibold text-red-400 border border-red-500/20">
              <AlertTriangle className="h-3.5 w-3.5" />
              {stats!.activeAlerts} active alert{stats!.activeAlerts !== 1 ? 's' : ''}
            </span>
          )}
          <button
            onClick={refreshAll}
            className="flex items-center gap-1.5 rounded-lg border border-border bg-card px-3 py-1.5 text-xs text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
          >
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </button>
        </div>
      </div>

      {/* ── Row 1: Primary KPI cards ─────────────────────────────────────── */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <StatCard label="Total Captures"  value={stats?.totalCaptures ?? 0}  icon={ShieldCheck} accent="text-cyan-400"   loading={statsLoading} sub={`${captures.filter(c=>c.status==='Verified').length} verified`} />
        <StatCard label="Pending Review"  value={stats?.pendingReview ?? 0}   icon={Clock}       accent="text-yellow-400" loading={statsLoading} sub="awaiting manual check" />
        <StatCard label="Verified Today"  value={stats?.verifiedToday ?? 0}   icon={CheckCircle} accent="text-green-400"  loading={statsLoading} sub="hash + signature OK" />
        <StatCard label="Active Alerts"   value={stats?.activeAlerts ?? 0}    icon={AlertTriangle} accent={stats?.activeAlerts ? 'text-red-400' : 'text-muted-foreground'} loading={statsLoading} sub="critical events <24h" />
      </div>

      {/* ── Row 2: Secondary KPI cards ───────────────────────────────────── */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <StatCard label="Exams"           value={exams.length}                icon={BookOpen}    accent="text-blue-400"   sub={`${activeExams} active`} />
        <StatCard label="Avg Score"       value={statistics ? `${statistics.averagePercentage.toFixed(1)}%` : '—'} icon={BarChart2} accent="text-violet-400" sub={`${statistics?.totalPapersScored ?? 0} papers scored`} />
        <StatCard label="Active Devices"  value={activeDevices}               icon={Cpu}         accent="text-orange-400" sub={`${pendingDevices} pending approval`} />
        <StatCard label="OCR Queue"       value={captures.filter(c=>c.status==='Uploaded').length} icon={Layers} accent="text-slate-400" sub="captures awaiting OCR" />
      </div>

      {/* ── Row 3: Capture Pipeline + Security Severity ──────────────────── */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">

        {/* Capture Status Donut */}
        <ChartCard title="Capture Pipeline Status">
          {captureStatusData.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">No captures yet</p>
          ) : (
            <div className="flex items-center gap-4">
              <ResponsiveContainer width="55%" height={200}>
                <PieChart>
                  <Pie
                    data={captureStatusData} cx="50%" cy="50%"
                    innerRadius={55} outerRadius={85}
                    paddingAngle={3} dataKey="value"
                    animationBegin={0} animationDuration={800}
                  >
                    {captureStatusData.map(entry => (
                      <Cell key={entry.name} fill={captureColors[entry.name]} stroke="transparent" />
                    ))}
                  </Pie>
                  <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [v, 'captures']} />
                </PieChart>
              </ResponsiveContainer>
              <div className="flex flex-col gap-2">
                {captureStatusData.map(entry => (
                  <div key={entry.name} className="flex items-center gap-2">
                    <span className="h-2.5 w-2.5 rounded-full shrink-0" style={{ background: captureColors[entry.name] }} />
                    <span className="text-xs text-muted-foreground w-16">{entry.name}</span>
                    <span className="text-xs font-bold text-foreground tabular-nums">{entry.value}</span>
                  </div>
                ))}
                <div className="mt-1 border-t border-border pt-2">
                  <span className="text-xs text-muted-foreground">Total: </span>
                  <span className="text-xs font-bold text-foreground">{captures.length}</span>
                </div>
              </div>
            </div>
          )}
        </ChartCard>

        {/* Security Events by Severity */}
        <ChartCard title="Security Threat Breakdown">
          {events.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">No security events</p>
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={securityBarData} margin={{ top: 4, right: 8, left: -16, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(222 47% 22%)" vertical={false} />
                <XAxis dataKey="severity" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis allowDecimals={false} tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={TOOLTIP_STYLE} cursor={{ fill: 'hsl(222 47% 18%)' }} />
                <Bar dataKey="count" name="Events" radius={[4, 4, 0, 0]} maxBarSize={48}>
                  {securityBarData.map(entry => (
                    <Cell key={entry.severity} fill={severityColors[entry.severity]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>
      </div>

      {/* ── Row 4: Score Distribution + Exam Overview ────────────────────── */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">

        {/* Score Distribution */}
        <ChartCard title="Score Distribution">
          {scoreBarData.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">No scores published yet</p>
          ) : (
            <div className="space-y-3">
              {/* Bucket summary */}
              <div className="grid grid-cols-4 gap-2">
                {scoreBuckets.map(b => (
                  <div key={b.range} className="rounded-lg bg-muted/50 p-2 text-center">
                    <p className="text-[10px] text-muted-foreground leading-tight">{b.range}</p>
                    <p className="text-lg font-bold text-foreground">{b.count}</p>
                  </div>
                ))}
              </div>
              {/* Bar chart */}
              <ResponsiveContainer width="100%" height={130}>
                <BarChart data={scoreBarData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(222 47% 22%)" vertical={false} />
                  <XAxis dataKey="label" tick={{ fill: '#94a3b8', fontSize: 10 }} axisLine={false} tickLine={false} />
                  <YAxis domain={[0, 100]} tick={{ fill: '#94a3b8', fontSize: 10 }} axisLine={false} tickLine={false} />
                  <Tooltip contentStyle={TOOLTIP_STYLE} formatter={(v) => [`${v}%`, 'Score']} cursor={{ fill: 'hsl(222 47% 18%)' }} />
                  <Bar dataKey="pct" name="Score %" radius={[3, 3, 0, 0]} maxBarSize={32}>
                    {scoreBarData.map(entry => (
                      <Cell key={entry.label}
                        fill={entry.pct >= 90 ? C.green : entry.pct >= 70 ? C.cyan : entry.pct >= 50 ? C.yellow : C.red}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
              <div className="flex items-center justify-between text-xs text-muted-foreground px-1">
                <span>High: <span className="font-semibold text-cyan-400">{statistics?.highestScore ?? '—'}</span></span>
                <span>Avg: <span className="font-semibold text-violet-400">{statistics?.averagePercentage.toFixed(1) ?? '—'}%</span></span>
                <span>Low: <span className="font-semibold text-yellow-400">{statistics?.lowestScore ?? '—'}</span></span>
              </div>
            </div>
          )}
        </ChartCard>

        {/* Exam Overview Radar */}
        <ChartCard title="Exam Overview">
          {exams.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">No exams yet</p>
          ) : (
            <div className="space-y-3">
              {/* Status counts */}
              <div className="flex gap-3">
                {examStatusData.map(d => (
                  <div key={d.status} className="flex-1 rounded-lg bg-muted/50 p-2 text-center">
                    <p className="text-[10px] text-muted-foreground">{d.status}</p>
                    <p className="text-xl font-bold" style={{ color: examStatusColors[d.status] }}>{d.count}</p>
                  </div>
                ))}
              </div>
              {/* Exam list */}
              <div className="space-y-2 max-h-[140px] overflow-y-auto pr-1">
                {[...exams].reverse().map(exam => (
                  <div key={exam.examId} className="flex items-center justify-between rounded-lg border border-border/60 bg-muted/20 px-3 py-2">
                    <div className="min-w-0 flex-1">
                      <p className="text-xs font-medium text-foreground truncate">{exam.name}</p>
                      <p className="text-[10px] text-muted-foreground">{exam.totalQuestions} questions</p>
                    </div>
                    <span
                      className="ml-3 shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold"
                      style={{
                        background: examStatusColors[exam.status] + '22',
                        color: examStatusColors[exam.status],
                      }}
                    >
                      {exam.status}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </ChartCard>
      </div>

      {/* ── Row 5: Activity Feed + Security Radar ────────────────────────── */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">

        {/* Recent Activity Feed */}
        <div className="lg:col-span-2">
          <ChartCard title="Recent Activity">
            {audit.length === 0 ? (
              <p className="py-4 text-sm text-muted-foreground">No audit entries</p>
            ) : (
              <div className="space-y-0 divide-y divide-border/50">
                {audit.slice(0, 8).map((entry, i) => (
                  <div key={entry.id ?? i} className="flex items-start gap-3 py-2.5 first:pt-0 last:pb-0">
                    <div className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-muted">
                      <ActionIcon action={entry.action} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="text-xs font-medium text-foreground">
                        {entry.action.replace(/([A-Z])/g, ' $1').trim()}
                      </p>
                      <p className="text-[10px] text-muted-foreground truncate">
                        {entry.userId === 'system' ? 'System' : entry.userId.substring(0, 8) + '…'}
                        {' · '}{entry.ipAddress}
                      </p>
                    </div>
                    <p className="shrink-0 text-[10px] text-muted-foreground tabular-nums">
                      {new Date(entry.occurredAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </ChartCard>
        </div>

        {/* Security Radar (event types) */}
        <ChartCard title="Threat Radar">
          {events.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">No events</p>
          ) : (
            (() => {
              const typeMap: Record<string, number> = {}
              events.forEach(e => { typeMap[e.eventType] = (typeMap[e.eventType] ?? 0) + 1 })
              const radarData = Object.entries(typeMap)
                .sort((a, b) => b[1] - a[1])
                .slice(0, 6)
                .map(([type, count]) => ({
                  subject: type.replace(/([A-Z])/g, ' $1').trim().split(' ').slice(0, 2).join(' '),
                  count,
                }))
              return (
                <ResponsiveContainer width="100%" height={210}>
                  <RadarChart data={radarData} margin={{ top: 0, right: 20, left: 20, bottom: 0 }}>
                    <PolarGrid stroke="hsl(222 47% 22%)" />
                    <PolarAngleAxis dataKey="subject" tick={{ fill: '#94a3b8', fontSize: 9 }} />
                    <Radar name="Count" dataKey="count" stroke={C.cyan} fill={C.cyan} fillOpacity={0.2} strokeWidth={2} />
                    <Tooltip contentStyle={TOOLTIP_STYLE} />
                  </RadarChart>
                </ResponsiveContainer>
              )
            })()
          )}
        </ChartCard>
      </div>
    </div>
  )
}
