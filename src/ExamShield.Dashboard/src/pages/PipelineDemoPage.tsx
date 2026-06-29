import { useState, useEffect, useRef } from 'react'
import {
  Camera, Hash, Shield, Upload, CheckCircle, Layers, Lock,
  ScanLine, Eye, Star, Send, AlertTriangle, Play, Pause,
  RotateCcw, Activity, ChevronRight, User,
} from 'lucide-react'
import { cn } from '../lib/utils'

interface PipelineStage {
  id: string
  title: string
  role: string
  roleColor: string
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  icon: React.ComponentType<any>
  dataIn: string
  dataOut: string
  securityControls: string[]
  auditEvent: string
  description: string
  duration: number
  tamperPoint?: boolean
}

const STAGES: PipelineStage[] = [
  {
    id: 'capture',
    title: 'Capture',
    role: 'Invigilator',
    roleColor: 'blue',
    icon: Camera,
    dataIn: 'Camera preview frame',
    dataOut: 'Raw JPEG bytes (4–8 MB)',
    securityControls: [
      'Document edge detection + perspective correction',
      'Anti-blur validation before acceptance',
      'Offline queue — syncs when network available',
    ],
    auditEvent: 'CAPTURE_INITIATED',
    description: 'The Invigilator uses the Flutter mobile app to photograph the answer sheet. The app detects document edges, auto-corrects perspective, and validates sharpness before committing the frame.',
    duration: 1200,
  },
  {
    id: 'hash',
    title: 'Hash',
    role: 'Device (on-device crypto)',
    roleColor: 'purple',
    icon: Hash,
    dataIn: 'Raw JPEG bytes',
    dataOut: 'SHA-256 hash (32 bytes hex)',
    securityControls: [
      'SHA-256 computed on raw bytes',
      'Computed before any network call',
      'Deterministic — same bytes → same hash',
    ],
    auditEvent: 'HASH_COMPUTED',
    description: 'SHA-256 is computed entirely on-device before any network activity. The hash commits the image at the moment of capture — any post-capture modification produces a mismatch at server verification.',
    duration: 600,
    tamperPoint: true,
  },
  {
    id: 'sign',
    title: 'Sign',
    role: 'Device Key (ECDSA P-256)',
    roleColor: 'purple',
    icon: Shield,
    dataIn: 'SHA-256 hash',
    dataOut: 'ECDSA P-256 / Ed25519 signature',
    securityControls: [
      'Signed with registered device private key',
      'Private key never leaves the device',
      'Binds device identity to the capture hash',
    ],
    auditEvent: 'SIGNATURE_CREATED',
    description: "The hash is signed with the device's registered private key. The server verifies against the registered public key — an unregistered device or an altered hash both fail signature verification.",
    duration: 500,
  },
  {
    id: 'upload',
    title: 'Upload',
    role: 'Invigilator → Server',
    roleColor: 'cyan',
    icon: Upload,
    dataIn: 'Image bytes + hash + signature + metadata',
    dataOut: 'HTTP POST /capture then /upload (two-phase)',
    securityControls: [
      'TLS 1.3 encryption in transit',
      'JWT authentication (Invigilator role)',
      'Two-phase: register metadata first, then bytes',
    ],
    auditEvent: 'UPLOAD_STARTED',
    description: 'Upload uses a two-phase protocol. Phase 1 registers the capture record (hash, signature, device ID, exam ID). Phase 2 sends the image bytes. This prevents orphaned uploads and makes every transfer auditable.',
    duration: 1500,
  },
  {
    id: 'verify',
    title: 'Verify',
    role: 'Server (integrity gate)',
    roleColor: 'green',
    icon: CheckCircle,
    dataIn: 'Received bytes + registered hash + signature',
    dataOut: 'Verified capture record / rejection + alert',
    securityControls: [
      'Server independently re-computes SHA-256',
      'Compares against registered hash',
      'Verifies ECDSA/Ed25519 signature',
      'Rejection + security event on any mismatch',
    ],
    auditEvent: 'VERIFICATION_PASSED',
    description: 'The server independently re-hashes the received bytes and verifies the signature against the registered device public key. Any mismatch — modified image, wrong device, or replay — is rejected and triggers a security alert.',
    duration: 800,
  },
  {
    id: 'watermark',
    title: 'Watermark',
    role: 'Server (pipeline)',
    roleColor: 'teal',
    icon: Layers,
    dataIn: 'Verified image bytes',
    dataOut: 'Watermarked image (invisible steganography)',
    securityControls: [
      'Invisible steganographic watermark embedded',
      'Encodes: examId, timestamp, nonce, hash, deviceId',
      'Watermark destruction signals downstream tampering',
    ],
    auditEvent: 'WATERMARK_EMBEDDED',
    description: 'An invisible watermark is embedded that encodes the exam ID, timestamp, nonce, original hash, and scanner ID. Forensic tools can extract and validate the watermark — destruction or mismatch signals that the stored image was tampered with after ingestion.',
    duration: 700,
  },
  {
    id: 'encrypt',
    title: 'Encrypt',
    role: 'Server (pipeline)',
    roleColor: 'orange',
    icon: Lock,
    dataIn: 'Watermarked image bytes',
    dataOut: 'AES-256-GCM ciphertext + envelope-encrypted DEK',
    securityControls: [
      'Per-image AES-256 DEK generated at upload time',
      'DEK envelope-encrypted with master key (KMS/Vault in prod)',
      'Raw MinIO/S3 access yields only ciphertext',
    ],
    auditEvent: 'IMAGE_ENCRYPTED',
    description: 'Every image is encrypted with a fresh AES-256-GCM key (DEK). The DEK is itself envelope-encrypted with the master key — stored in HashiCorp Vault, AWS KMS, or Azure Key Vault in production. Direct object-storage access returns only ciphertext.',
    duration: 600,
  },
  {
    id: 'ocr',
    title: 'OCR',
    role: 'OCR Engine (system)',
    roleColor: 'yellow',
    icon: ScanLine,
    dataIn: 'Decrypted image bytes',
    dataOut: 'Extracted answers + per-question confidence scores',
    securityControls: [
      'Read-only access — cannot modify stored images',
      'Low-confidence results → Manual Review queue',
      'No answer silently discarded',
    ],
    auditEvent: 'OCR_COMPLETED',
    description: 'The OCR Engine decrypts the image transiently, detects bubble positions, extracts answer selections, and assigns confidence scores per question. Results below the threshold are routed to the Manual Review queue.',
    duration: 2000,
  },
  {
    id: 'review',
    title: 'Review',
    role: 'Manual Reviewer',
    roleColor: 'amber',
    icon: Eye,
    dataIn: 'Original image + OCR predictions',
    dataOut: 'Confirmed answer interpretation (immutable record)',
    securityControls: [
      'Pixel lock — no image editing possible',
      'Records interpretation only; never replaces original',
      'Every decision creates an immutable audit record',
      'Review Supervisor approval required for disputes',
    ],
    auditEvent: 'REVIEW_SUBMITTED',
    description: 'The reviewer sees the original image alongside OCR predictions. The interface enforces pixel lock — they cannot paint, erase, or modify any pixel. Their only action is selecting the correct answer interpretation, which is recorded immutably.',
    duration: 3000,
  },
  {
    id: 'score',
    title: 'Score',
    role: 'Scoring Engine (system)',
    roleColor: 'emerald',
    icon: Star,
    dataIn: 'Confirmed answers + official answer key',
    dataOut: 'Score, percentile, statistics',
    securityControls: [
      'Reads finalized answers only — cannot modify interpretations',
      'SoD: strictly separated from reviewer role',
    ],
    auditEvent: 'SCORE_CALCULATED',
    description: 'The Scoring Engine compares confirmed answers against the official answer key and calculates scores, percentiles, and statistics. It operates only after all reviews are finalized and is fully separated from any reviewing role.',
    duration: 800,
  },
  {
    id: 'publish',
    title: 'Publish',
    role: 'Result Publisher',
    roleColor: 'blue',
    icon: Send,
    dataIn: 'Finalized scores + metadata',
    dataOut: 'Published results (student-visible)',
    securityControls: [
      'Cannot modify scores — read-only access to scoring output',
      'SoD: strictly separated from scorer role',
      'Scheduled or manual release with audit trail',
    ],
    auditEvent: 'RESULTS_PUBLISHED',
    description: "The Result Publisher makes finalized results visible to students and generates official reports. They have zero ability to alter scores — their only permitted action is triggering or scheduling the release.",
    duration: 600,
  },
]

const ROLE_COLORS: Record<string, string> = {
  blue:    'bg-blue-500/20 text-blue-400 border-blue-500/30',
  purple:  'bg-purple-500/20 text-purple-400 border-purple-500/30',
  cyan:    'bg-cyan-500/20 text-cyan-400 border-cyan-500/30',
  green:   'bg-green-500/20 text-green-400 border-green-500/30',
  teal:    'bg-teal-500/20 text-teal-400 border-teal-500/30',
  orange:  'bg-orange-500/20 text-orange-400 border-orange-500/30',
  yellow:  'bg-yellow-500/20 text-yellow-400 border-yellow-500/30',
  amber:   'bg-amber-500/20 text-amber-400 border-amber-500/30',
  emerald: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30',
}

interface AuditEntry {
  id: number
  ts: string
  event: string
  stage: string
  status: 'ok' | 'alert' | 'pending'
}

export default function PipelineDemoPage() {
  const [activeStage, setActiveStage]       = useState(-1)
  const [selectedStage, setSelectedStage]   = useState(0)
  const [isPlaying, setIsPlaying]           = useState(false)
  const [tamperMode, setTamperMode]         = useState(false)
  const [tamperDetected, setTamperDetected] = useState(false)
  const [auditLog, setAuditLog]             = useState<AuditEntry[]>([])
  const [completedStages, setCompletedStages] = useState<Set<number>>(new Set())

  const timerRef      = useRef<ReturnType<typeof setTimeout> | null>(null)
  const tamperModeRef = useRef(tamperMode)
  const runnerRef     = useRef<((index: number) => void) | null>(null)
  const auditIdRef    = useRef(0)

  useEffect(() => { tamperModeRef.current = tamperMode }, [tamperMode])

  useEffect(() => {
    runnerRef.current = (index: number) => {
      if (index >= STAGES.length) {
        setIsPlaying(false)
        setActiveStage(-1)
        return
      }
      const stage = STAGES[index]
      setActiveStage(index)
      setSelectedStage(index)

      const ts = new Date().toLocaleTimeString('en-US', { hour12: false })
      const id = ++auditIdRef.current
      setAuditLog(prev => [...prev, { id, ts, event: stage.auditEvent, stage: stage.title, status: 'pending' }])

      timerRef.current = setTimeout(() => {
        if (tamperModeRef.current && stage.tamperPoint) {
          setTamperDetected(true)
          setAuditLog(prev =>
            prev.map(e => e.id === id ? { ...e, status: 'alert', event: 'HASH_TAMPER_DETECTED' } : e)
          )
          const ts2 = new Date().toLocaleTimeString('en-US', { hour12: false })
          setAuditLog(prev => [...prev, {
            id: ++auditIdRef.current,
            ts: ts2,
            event: 'VERIFICATION_FAILED — hash mismatch, upload rejected',
            stage: 'Verify',
            status: 'alert',
          }])
          setIsPlaying(false)
          setActiveStage(4)
          return
        }
        setCompletedStages(prev => new Set([...prev, index]))
        setAuditLog(prev => prev.map(e => e.id === id ? { ...e, status: 'ok' } : e))
        runnerRef.current?.(index + 1)
      }, stage.duration)
    }
  })

  const resetSimulation = () => {
    if (timerRef.current) clearTimeout(timerRef.current)
    setActiveStage(-1)
    setIsPlaying(false)
    setTamperDetected(false)
    setAuditLog([])
    setCompletedStages(new Set())
    auditIdRef.current = 0
  }

  const handlePlay = () => {
    if (isPlaying) {
      if (timerRef.current) clearTimeout(timerRef.current)
      setIsPlaying(false)
      return
    }
    const startFrom = activeStage < 0 || activeStage >= STAGES.length - 1 ? 0 : activeStage + 1
    if (startFrom === 0) {
      setCompletedStages(new Set())
      setAuditLog([])
      setTamperDetected(false)
      auditIdRef.current = 0
    }
    setIsPlaying(true)
    runnerRef.current?.(startFrom)
  }

  useEffect(() => () => { if (timerRef.current) clearTimeout(timerRef.current) }, [])

  const stage = STAGES[selectedStage]

  return (
    <div className="flex flex-col gap-5 p-6 min-h-full">

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gradient">Pipeline Showcase</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Interactive walkthrough of ExamShield's 11-stage secure exam scanning pipeline
          </p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <button
            onClick={() => { resetSimulation(); setTamperMode(t => !t) }}
            className={cn(
              'flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium border transition-all',
              tamperMode
                ? 'bg-red-500/20 text-red-400 border-red-500/40 shadow-[0_0_14px_rgba(239,68,68,0.25)]'
                : 'glass border-border/50 text-muted-foreground hover:text-foreground',
            )}
          >
            <AlertTriangle className="h-3.5 w-3.5" />
            {tamperMode ? 'Tamper Mode ON' : 'Simulate Tamper'}
          </button>
          <button
            onClick={resetSimulation}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium glass border border-border/50 text-muted-foreground hover:text-foreground transition-all"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            Reset
          </button>
          <button
            onClick={handlePlay}
            className="flex items-center gap-1.5 px-4 py-1.5 rounded-lg text-sm font-semibold bg-primary/20 border border-primary/40 text-primary hover:bg-primary/30 transition-all shadow-glow-sm"
          >
            {isPlaying ? <Pause className="h-3.5 w-3.5" /> : <Play className="h-3.5 w-3.5" />}
            {isPlaying ? 'Pause' : activeStage >= 0 ? 'Resume' : 'Run Simulation'}
          </button>
        </div>
      </div>

      {/* Tamper alert */}
      {tamperDetected && (
        <div className="flex items-center gap-3 px-4 py-3 rounded-xl bg-red-500/10 border border-red-500/40 text-red-400">
          <AlertTriangle className="h-5 w-5 shrink-0" />
          <div>
            <p className="font-semibold text-sm">SECURITY ALERT: Hash Mismatch Detected</p>
            <p className="text-xs text-red-400/70 mt-0.5">
              Upload rejected at verification gate. Security event dispatched to Security Center and alert channels.
            </p>
          </div>
        </div>
      )}

      {/* Pipeline flow */}
      <div className="glass-lg rounded-2xl p-4 overflow-x-auto">
        <div className="flex items-center min-w-max gap-0">
          {STAGES.map((s, i) => {
            const Icon = s.icon
            const isActive   = activeStage === i
            const isDone     = completedStages.has(i)
            const isSelected = selectedStage === i
            const isError    = tamperDetected && s.tamperPoint

            return (
              <div key={s.id} className="flex items-center">
                <button
                  onClick={() => setSelectedStage(i)}
                  className={cn(
                    'flex flex-col items-center gap-1.5 px-3 py-2.5 rounded-xl transition-all group',
                    isSelected ? 'bg-primary/15 ring-1 ring-primary/40' : 'hover:bg-muted/30',
                  )}
                >
                  <div className={cn(
                    'relative flex h-10 w-10 items-center justify-center rounded-xl transition-all',
                    isError
                      ? 'bg-red-500/20 ring-2 ring-red-500/60 shadow-[0_0_16px_rgba(239,68,68,0.4)]'
                      : isActive
                        ? 'bg-primary/20 ring-2 ring-primary/60 shadow-glow animate-pulse'
                        : isDone
                          ? 'bg-green-500/15 ring-1 ring-green-500/40'
                          : 'bg-muted/40 ring-1 ring-border/30 group-hover:ring-border/60',
                  )}>
                    <Icon className={cn(
                      'h-4 w-4',
                      isError  ? 'text-red-400'
                      : isActive ? 'text-primary'
                      : isDone  ? 'text-green-400'
                      : 'text-muted-foreground',
                    )} />
                    {isDone && !isError && (
                      <span className="absolute -top-1 -right-1 h-3 w-3 rounded-full bg-green-500 border-2 border-background" />
                    )}
                    {isError && (
                      <span className="absolute -top-1 -right-1 h-3 w-3 rounded-full bg-red-500 border-2 border-background animate-pulse" />
                    )}
                  </div>
                  <span className={cn(
                    'text-[10px] font-medium whitespace-nowrap',
                    isActive ? 'text-primary'
                    : isDone  ? 'text-green-400'
                    : 'text-muted-foreground',
                  )}>
                    {s.title}
                  </span>
                </button>

                {i < STAGES.length - 1 && (
                  <ChevronRight className={cn(
                    'h-4 w-4 mx-0.5 shrink-0 transition-colors',
                    completedStages.has(i) ? 'text-green-400/50' : 'text-border/40',
                  )} />
                )}
              </div>
            )
          })}
        </div>
      </div>

      {/* Detail + Audit */}
      <div className="grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-4 flex-1">

        {/* Stage detail */}
        <div className="glass-lg rounded-2xl p-5 space-y-4">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <div className="flex items-center gap-2 mb-1 flex-wrap">
                <span className={cn(
                  'inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-full border shrink-0',
                  ROLE_COLORS[stage.roleColor] ?? ROLE_COLORS.blue,
                )}>
                  <User className="h-2.5 w-2.5" />
                  {stage.role}
                </span>
                <span className="text-[11px] text-muted-foreground font-mono">
                  Stage {selectedStage + 1} / {STAGES.length}
                </span>
              </div>
              <h2 className="text-xl font-bold">{stage.title}</h2>
              <p className="text-sm text-muted-foreground mt-1 leading-relaxed">{stage.description}</p>
            </div>
            <div className="shrink-0 flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10">
              <stage.icon className="h-5 w-5 text-primary" />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-xl bg-muted/20 p-3">
              <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground mb-1.5">
                Data In
              </p>
              <p className="text-sm font-mono text-foreground/80 leading-relaxed">{stage.dataIn}</p>
            </div>
            <div className="rounded-xl bg-muted/20 p-3">
              <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground mb-1.5">
                Data Out
              </p>
              <p className="text-sm font-mono text-foreground/80 leading-relaxed">{stage.dataOut}</p>
            </div>
          </div>

          <div className="rounded-xl bg-muted/20 p-3">
            <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground mb-2">
              Security Controls
            </p>
            <ul className="space-y-2">
              {stage.securityControls.map(ctrl => (
                <li key={ctrl} className="flex items-start gap-2 text-sm">
                  <span className="mt-1.5 h-1.5 w-1.5 rounded-full bg-green-400 shrink-0" />
                  <span className="text-foreground/80">{ctrl}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="rounded-xl bg-primary/5 border border-primary/20 p-3">
            <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Audit Event Emitted
            </p>
            <p className="text-sm font-mono text-primary">{stage.auditEvent}</p>
          </div>
        </div>

        {/* Audit log */}
        <div className="glass-lg rounded-2xl p-4 flex flex-col min-h-64">
          <div className="flex items-center gap-2 mb-3 shrink-0">
            <Activity className="h-4 w-4 text-primary" />
            <h3 className="text-sm font-semibold">Live Audit Log</h3>
            <span className="ml-auto text-[11px] text-muted-foreground">{auditLog.length} events</span>
          </div>
          <div className="flex-1 overflow-y-auto space-y-1.5 min-h-0">
            {auditLog.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center mt-10 px-4">
                Press <span className="text-primary font-medium">Run Simulation</span> to see audit events populate in real-time
              </p>
            ) : (
              auditLog.map(entry => (
                <div
                  key={entry.id}
                  className={cn(
                    'flex items-start gap-2 px-3 py-2 rounded-lg text-xs transition-all',
                    entry.status === 'alert'
                      ? 'bg-red-500/10 border border-red-500/30'
                      : entry.status === 'pending'
                        ? 'bg-yellow-500/10 border border-yellow-500/20'
                        : 'bg-green-500/5 border border-green-500/15',
                  )}
                >
                  <span className={cn(
                    'mt-1 h-1.5 w-1.5 rounded-full shrink-0',
                    entry.status === 'alert'   ? 'bg-red-400'
                    : entry.status === 'pending' ? 'bg-yellow-400 animate-pulse'
                    : 'bg-green-400',
                  )} />
                  <div className="min-w-0">
                    <p className={cn(
                      'font-mono font-medium break-all',
                      entry.status === 'alert'   ? 'text-red-400'
                      : entry.status === 'pending' ? 'text-yellow-400'
                      : 'text-green-400',
                    )}>
                      {entry.event}
                    </p>
                    <p className="text-muted-foreground mt-0.5">{entry.ts} · {entry.stage}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
