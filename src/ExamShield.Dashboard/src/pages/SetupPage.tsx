import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useSetupStatus } from '../hooks/useSetupStatus'

// ── Types ──────────────────────────────────────────────────────────────────

type Step = 'system-check' | 'admin-account' | 'options' | 'complete'

interface AdminForm {
  email: string
  displayName: string
  password: string
  confirmPassword: string
}

interface FormErrors {
  email?: string
  displayName?: string
  password?: string
  confirmPassword?: string
}

// ── Helpers ────────────────────────────────────────────────────────────────

const STEPS: { id: Step; label: string; num: number }[] = [
  { id: 'system-check',  label: 'System Check',  num: 1 },
  { id: 'admin-account', label: 'Admin Account', num: 2 },
  { id: 'options',       label: 'Options',        num: 3 },
  { id: 'complete',      label: 'Complete',       num: 4 },
]

const STATUS_COLOR: Record<string, string> = {
  Healthy:   'text-green-400',
  Degraded:  'text-yellow-400',
  Unhealthy: 'text-red-400',
}

const STATUS_DOT: Record<string, string> = {
  Healthy:   'bg-green-400',
  Degraded:  'bg-yellow-400',
  Unhealthy: 'bg-red-500',
}

const SERVICE_LABELS: Record<string, string> = {
  api:      'API Server',
  postgres: 'PostgreSQL',
  redis:    'Redis',
  rabbitmq: 'RabbitMQ',
  minio:    'MinIO Storage',
}

function passwordStrength(pw: string): { score: number; label: string; color: string } {
  let score = 0
  if (pw.length >= 8)  score++
  if (pw.length >= 12) score++
  if (/[A-Z]/.test(pw)) score++
  if (/[0-9]/.test(pw)) score++
  if (/[^A-Za-z0-9]/.test(pw)) score++
  const levels = [
    { label: 'Too short', color: 'bg-red-500' },
    { label: 'Weak',      color: 'bg-red-400' },
    { label: 'Fair',      color: 'bg-yellow-400' },
    { label: 'Good',      color: 'bg-blue-400' },
    { label: 'Strong',    color: 'bg-green-400' },
    { label: 'Very strong',color:'bg-green-500' },
  ]
  return { score, ...levels[Math.min(score, levels.length - 1)] }
}

function validateForm(form: AdminForm): FormErrors {
  const errors: FormErrors = {}
  if (!form.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
    errors.email = 'Valid email address required'
  if (!form.displayName.trim())
    errors.displayName = 'Display name required'
  if (form.password.length < 8)
    errors.password = 'Minimum 8 characters'
  else if (!/[A-Z]/.test(form.password))
    errors.password = 'Must contain an uppercase letter'
  else if (!/[0-9]/.test(form.password))
    errors.password = 'Must contain a digit'
  else if (!/[^A-Za-z0-9]/.test(form.password))
    errors.password = 'Must contain a special character'
  if (form.confirmPassword !== form.password)
    errors.confirmPassword = 'Passwords do not match'
  return errors
}

// ── Sub-components ─────────────────────────────────────────────────────────

function StepIndicator({ current }: { current: Step }) {
  const currentNum = STEPS.find(s => s.id === current)!.num
  return (
    <div className="flex items-center justify-center gap-0 mb-8">
      {STEPS.map((step, i) => (
        <div key={step.id} className="flex items-center">
          <div className={`flex items-center justify-center w-8 h-8 rounded-full border-2 text-sm font-semibold transition-all
            ${step.num < currentNum  ? 'bg-cyan-500 border-cyan-500 text-slate-900' :
              step.num === currentNum ? 'bg-slate-800 border-cyan-400 text-cyan-400' :
                                        'bg-slate-800 border-slate-600 text-slate-500'}`}>
            {step.num < currentNum ? '✓' : step.num}
          </div>
          <div className={`hidden sm:block ml-2 text-xs font-medium
            ${step.num === currentNum ? 'text-cyan-400' : step.num < currentNum ? 'text-slate-400' : 'text-slate-600'}`}>
            {step.label}
          </div>
          {i < STEPS.length - 1 && (
            <div className={`w-8 sm:w-16 h-0.5 mx-2 sm:mx-3 transition-all
              ${step.num < currentNum ? 'bg-cyan-500' : 'bg-slate-700'}`} />
          )}
        </div>
      ))}
    </div>
  )
}

function ServiceBadge({ name, status }: { name: string; status: string }) {
  return (
    <div className="flex items-center justify-between py-2.5 px-4 rounded-lg bg-slate-800/60 border border-slate-700">
      <span className="text-slate-300 text-sm font-medium">{SERVICE_LABELS[name] ?? name}</span>
      <div className="flex items-center gap-2">
        <span className={`w-2 h-2 rounded-full ${STATUS_DOT[status] ?? 'bg-slate-500'}`} />
        <span className={`text-xs font-semibold ${STATUS_COLOR[status] ?? 'text-slate-400'}`}>{status}</span>
      </div>
    </div>
  )
}

function InputField({
  label, type = 'text', value, onChange, error, placeholder, autoComplete
}: {
  label: string; type?: string; value: string; onChange: (v: string) => void
  error?: string; placeholder?: string; autoComplete?: string
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-300 mb-1.5">{label}</label>
      <input
        type={type}
        value={value}
        onChange={e => onChange(e.target.value)}
        placeholder={placeholder}
        autoComplete={autoComplete}
        className={`w-full px-3 py-2.5 rounded-lg bg-slate-800 border text-slate-100
          placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-cyan-500/50 transition-colors
          ${error ? 'border-red-500' : 'border-slate-600 focus:border-cyan-500'}`}
      />
      {error && <p className="mt-1 text-xs text-red-400">{error}</p>}
    </div>
  )
}

// ── Steps ──────────────────────────────────────────────────────────────────

function SystemCheckStep({
  onNext
}: {
  onNext: () => void
}) {
  const { status, loading, error, refresh } = useSetupStatus()

  const checks = status?.checks ?? {}
  const allCriticalOk = checks['api'] === 'Healthy' && checks['postgres'] === 'Healthy'
  const hasAnyUnhealthy = Object.values(checks).some(v => v === 'Unhealthy')

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-slate-100 mb-1">System Requirements</h2>
        <p className="text-slate-400 text-sm">Verifying that all required services are reachable before continuing.</p>
      </div>

      {loading && (
        <div className="flex items-center gap-3 py-8 justify-center text-slate-400">
          <div className="w-5 h-5 border-2 border-cyan-400 border-t-transparent rounded-full animate-spin" />
          <span className="text-sm">Checking services…</span>
        </div>
      )}

      {error && (
        <div className="p-4 rounded-lg bg-red-900/30 border border-red-700 text-red-400 text-sm">
          Cannot reach the API: {error}
          <button onClick={refresh} className="ml-3 underline hover:text-red-200">Retry</button>
        </div>
      )}

      {!loading && !error && (
        <div className="space-y-2">
          {Object.entries(checks).map(([name, status]) => (
            <ServiceBadge key={name} name={name} status={status} />
          ))}
        </div>
      )}

      {!loading && hasAnyUnhealthy && (
        <div className="p-3 rounded-lg bg-yellow-900/30 border border-yellow-700 text-yellow-300 text-sm">
          Some services are unhealthy. You can continue if PostgreSQL and the API are healthy, but full functionality may be limited.
        </div>
      )}

      <div className="flex justify-between pt-2">
        <button
          onClick={refresh}
          disabled={loading}
          className="px-4 py-2 text-sm rounded-lg border border-slate-600 text-slate-300 hover:border-slate-400 hover:text-slate-100 disabled:opacity-50 transition-colors"
        >
          Refresh
        </button>
        <button
          onClick={onNext}
          disabled={loading || !allCriticalOk}
          className="px-6 py-2 text-sm font-semibold rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          Next →
        </button>
      </div>
    </div>
  )
}

function AdminAccountStep({
  form, errors, onChange, onNext, onBack
}: {
  form: AdminForm
  errors: FormErrors
  onChange: (field: keyof AdminForm, value: string) => void
  onNext: () => void
  onBack: () => void
}) {
  const strength = passwordStrength(form.password)
  const hasErrors = Object.keys(errors).length > 0
  const isComplete = form.email && form.displayName && form.password && form.confirmPassword

  return (
    <div className="space-y-5">
      <div>
        <h2 className="text-xl font-bold text-slate-100 mb-1">Create Super Administrator</h2>
        <p className="text-slate-400 text-sm">This account has full system access. Use a strong password and store it securely.</p>
      </div>

      <InputField
        label="Email Address"
        type="email"
        value={form.email}
        onChange={v => onChange('email', v)}
        error={errors.email}
        placeholder="admin@yourorganization.com"
        autoComplete="email"
      />

      <InputField
        label="Display Name"
        value={form.displayName}
        onChange={v => onChange('displayName', v)}
        error={errors.displayName}
        placeholder="System Administrator"
        autoComplete="name"
      />

      <div>
        <InputField
          label="Password"
          type="password"
          value={form.password}
          onChange={v => onChange('password', v)}
          error={errors.password}
          placeholder="Min. 8 chars, upper, digit, special"
          autoComplete="new-password"
        />
        {form.password && (
          <div className="mt-2">
            <div className="flex gap-1 mb-1">
              {[1,2,3,4,5].map(i => (
                <div key={i} className={`h-1 flex-1 rounded-full transition-all
                  ${i <= strength.score ? strength.color : 'bg-slate-700'}`} />
              ))}
            </div>
            <p className={`text-xs ${strength.score >= 4 ? 'text-green-400' : strength.score >= 3 ? 'text-yellow-400' : 'text-slate-500'}`}>
              {strength.label}
            </p>
          </div>
        )}
      </div>

      <InputField
        label="Confirm Password"
        type="password"
        value={form.confirmPassword}
        onChange={v => onChange('confirmPassword', v)}
        error={errors.confirmPassword}
        placeholder="Re-enter password"
        autoComplete="new-password"
      />

      <div className="flex justify-between pt-2">
        <button onClick={onBack} className="px-4 py-2 text-sm rounded-lg border border-slate-600 text-slate-300 hover:border-slate-400 transition-colors">
          ← Back
        </button>
        <button
          onClick={onNext}
          disabled={hasErrors || !isComplete}
          className="px-6 py-2 text-sm font-semibold rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          Next →
        </button>
      </div>
    </div>
  )
}

function OptionsStep({
  seedDemoData, onToggle, onBack, onSubmit, submitting, submitError
}: {
  seedDemoData: boolean
  onToggle: () => void
  onBack: () => void
  onSubmit: () => void
  submitting: boolean
  submitError: string | null
}) {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-slate-100 mb-1">Setup Options</h2>
        <p className="text-slate-400 text-sm">Configure how you want the system initialized.</p>
      </div>

      <div
        onClick={onToggle}
        className={`flex items-start gap-4 p-4 rounded-xl border-2 cursor-pointer transition-all
          ${seedDemoData ? 'border-cyan-500 bg-cyan-900/20' : 'border-slate-600 bg-slate-800/40 hover:border-slate-500'}`}
      >
        <div className={`mt-0.5 w-5 h-5 rounded-md border-2 flex items-center justify-center flex-shrink-0 transition-all
          ${seedDemoData ? 'bg-cyan-500 border-cyan-500' : 'border-slate-500'}`}>
          {seedDemoData && <span className="text-slate-900 text-xs font-bold">✓</span>}
        </div>
        <div>
          <p className="text-slate-100 font-semibold mb-1">Load Demo Data</p>
          <p className="text-slate-400 text-sm leading-relaxed">
            Populates the system with sample exams, answer sheets, OCR results, scores, and audit logs.
            Ideal for evaluation and testing. All demo users share the password <code className="text-cyan-400">Demo@1234</code>.
          </p>
          <div className="mt-2 flex flex-wrap gap-2">
            {['26 demo users', '3 exams', '18 captures', 'Full audit trail', 'Security events'].map(tag => (
              <span key={tag} className="px-2 py-0.5 text-xs rounded-full bg-slate-700 text-slate-300">{tag}</span>
            ))}
          </div>
        </div>
      </div>

      {!seedDemoData && (
        <div className="p-3 rounded-lg bg-blue-900/20 border border-blue-800 text-blue-300 text-sm">
          Clean installation — only your Super Administrator account will be created. Add users and exams after login.
        </div>
      )}

      {submitError && (
        <div className="p-3 rounded-lg bg-red-900/30 border border-red-700 text-red-400 text-sm">
          {submitError}
        </div>
      )}

      <div className="flex justify-between pt-2">
        <button onClick={onBack} disabled={submitting} className="px-4 py-2 text-sm rounded-lg border border-slate-600 text-slate-300 hover:border-slate-400 disabled:opacity-50 transition-colors">
          ← Back
        </button>
        <button
          onClick={onSubmit}
          disabled={submitting}
          className="px-6 py-2 text-sm font-semibold rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white disabled:opacity-60 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
        >
          {submitting && <span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />}
          {submitting ? 'Setting up…' : 'Complete Setup'}
        </button>
      </div>
    </div>
  )
}

function CompleteStep({ adminEmail }: { adminEmail: string }) {
  const navigate = useNavigate()
  return (
    <div className="text-center space-y-6">
      <div className="w-16 h-16 mx-auto rounded-full bg-green-500/20 flex items-center justify-center">
        <svg className="w-8 h-8 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
        </svg>
      </div>

      <div>
        <h2 className="text-2xl font-bold text-slate-100 mb-2">ExamShield is Ready</h2>
        <p className="text-slate-400 text-sm">Setup completed successfully. You can now log in with your Super Administrator account.</p>
      </div>

      <div className="text-left p-4 rounded-xl bg-slate-800/60 border border-slate-700 space-y-2">
        <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold mb-3">Login Credentials</p>
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-sm">Email</span>
          <span className="text-cyan-400 font-mono text-sm">{adminEmail}</span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-slate-400 text-sm">Role</span>
          <span className="text-slate-200 text-sm">Super Administrator</span>
        </div>
      </div>

      <div className="p-3 rounded-lg bg-amber-900/20 border border-amber-700/50 text-amber-300 text-xs text-left">
        Store your password securely. This setup wizard is now permanently disabled.
      </div>

      <button
        onClick={() => navigate('/login')}
        className="w-full py-3 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white font-semibold transition-colors"
      >
        Go to Login →
      </button>
    </div>
  )
}

// ── Main Page ──────────────────────────────────────────────────────────────

export default function SetupPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState<Step>('system-check')
  const [form, setForm] = useState<AdminForm>({ email: '', displayName: '', password: '', confirmPassword: '' })
  const [formErrors, setFormErrors] = useState<FormErrors>({})
  const [seedDemoData, setSeedDemoData] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [completedEmail, setCompletedEmail] = useState('')

  // Redirect away if setup already completed
  const { status } = useSetupStatus()
  useEffect(() => {
    if (status && !status.isFirstRun) navigate('/', { replace: true })
  }, [status, navigate])

  const updateForm = useCallback((field: keyof AdminForm, value: string) => {
    setForm(prev => {
      const updated = { ...prev, [field]: value }
      setFormErrors(validateForm(updated))
      return updated
    })
  }, [])

  const handleAdminNext = () => {
    const errors = validateForm(form)
    setFormErrors(errors)
    if (Object.keys(errors).length === 0) setStep('options')
  }

  const handleSubmit = async () => {
    setSubmitting(true)
    setSubmitError(null)
    try {
      await api.completeSetup({
        adminEmail: form.email,
        adminDisplayName: form.displayName,
        adminPassword: form.password,
        seedDemoData,
      })
      setCompletedEmail(form.email)
      setStep('complete')
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Setup failed'
      setSubmitError(msg.includes('409') ? 'Setup has already been completed.' : msg)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen bg-slate-950 flex flex-col items-center justify-center p-4">
      {/* Header */}
      <div className="mb-8 text-center">
        <div className="flex items-center justify-center gap-2 mb-3">
          <div className="w-8 h-8 rounded-lg bg-cyan-500 flex items-center justify-center">
            <svg className="w-5 h-5 text-slate-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <span className="text-xl font-bold text-slate-100">ExamShield</span>
        </div>
        <p className="text-slate-400 text-sm">First-Time Setup</p>
      </div>

      {/* Card */}
      <div className="w-full max-w-lg bg-slate-900/80 border border-slate-700/60 rounded-2xl p-6 sm:p-8 shadow-2xl backdrop-blur">
        {step !== 'complete' && <StepIndicator current={step} />}

        {step === 'system-check' && (
          <SystemCheckStep onNext={() => setStep('admin-account')} />
        )}

        {step === 'admin-account' && (
          <AdminAccountStep
            form={form}
            errors={formErrors}
            onChange={updateForm}
            onNext={handleAdminNext}
            onBack={() => setStep('system-check')}
          />
        )}

        {step === 'options' && (
          <OptionsStep
            seedDemoData={seedDemoData}
            onToggle={() => setSeedDemoData(p => !p)}
            onBack={() => setStep('admin-account')}
            onSubmit={handleSubmit}
            submitting={submitting}
            submitError={submitError}
          />
        )}

        {step === 'complete' && (
          <CompleteStep adminEmail={completedEmail} />
        )}
      </div>

      <p className="mt-6 text-slate-600 text-xs">ExamShield · Secure Exam Scanning System</p>
    </div>
  )
}
