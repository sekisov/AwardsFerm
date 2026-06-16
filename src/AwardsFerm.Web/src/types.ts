export type SessionStatus =
  | 'Idle'
  | 'Starting'
  | 'Running'
  | 'Completed'
  | 'Failed'
  | 'Stopped'

export interface SessionInfo {
  id: string
  profileId: string
  autoRestart: boolean
  status: SessionStatus
  currentStep: number
  totalSteps: number
  currentStepName: string
  errorMessage?: string
  startedAt?: string
  finishedAt?: string
  logs: string[]
}

export type SessionEventType =
  | 'Log'
  | 'StepChanged'
  | 'Screenshot'
  | 'StatusChanged'
  | 'Completed'
  | 'Failed'

export interface SessionEvent {
  sessionId: string
  type: SessionEventType
  message?: string
  currentStep?: number
  totalSteps?: number
  stepName?: string
  status?: SessionStatus
  screenshotBase64?: string
  timestamp: string
}

export interface SessionSlotConfig {
  profileId: string
  label: string
  scheduleEnabled: boolean
  scheduledStartMsk?: string | null
}

export const DEFAULT_SESSION_SLOTS: SessionSlotConfig[] = [
  { profileId: 'session-001', label: 'Сессия 1', scheduleEnabled: false },
  { profileId: 'session-002', label: 'Сессия 2', scheduleEnabled: false },
]

export interface SlotState {
  session: SessionInfo | null
  logs: string[]
  screenshot: string | null
  loading: boolean
}

export interface RsyaPeriodStats {
  reward: number
  shows: number
  clicks: number
  hits: number
  fillRate?: number
}

export interface RsyaDailyPoint {
  date: string
  reward: number
  shows: number
  clicks: number
}

export interface RsyaDashboard {
  configured: boolean
  error?: string
  currency: string
  reportTitle?: string
  today: RsyaPeriodStats
  yesterday: RsyaPeriodStats
  thisMonth: RsyaPeriodStats
  dailyChart: RsyaDailyPoint[]
  updatedAt: string
}

export function createEmptySlotState(): SlotState {
  return { session: null, logs: [], screenshot: null, loading: false }
}
