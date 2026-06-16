import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  addSlot,
  checkApiHealth,
  createSessionHub,
  deleteSlot,
  ensureHubConnected,
  fetchSessions,
  fetchSlots,
  startSession,
  stopSession,
  updateSlot,
} from './api'
import {
  DEFAULT_SESSION_SLOTS,
  createEmptySlotState,
  type SessionEvent,
  type SessionSlotConfig,
  type SessionStatus,
  type SlotState,
} from './types'
import { normalizeStatus, statusCssClass } from './utils/session'
import { RsyaStatsPanel } from './components/RsyaStatsPanel'
import './App.css'

const statusLabels: Record<SessionStatus, string> = {
  Idle: 'Ожидание',
  Starting: 'Запуск',
  Running: 'Выполняется',
  Completed: 'Завершено',
  Failed: 'Ошибка',
  Stopped: 'Остановлено',
}

function App() {
  const [slotConfigs, setSlotConfigs] = useState<SessionSlotConfig[]>(DEFAULT_SESSION_SLOTS)
  const [slots, setSlots] = useState<Record<string, SlotState>>(() =>
    Object.fromEntries(DEFAULT_SESSION_SLOTS.map((s) => [s.profileId, createEmptySlotState()])),
  )
  const [notice, setNotice] = useState<{ kind: 'error' | 'success'; text: string } | null>(null)
  const [confirmState, setConfirmState] = useState<{
    title: string
    message: string
    confirmText: string
    cancelText: string
  } | null>(null)
  const [connected, setConnected] = useState(false)
  const [apiUp, setApiUp] = useState(false)
  const [mskTime, setMskTime] = useState('')
  const [addingSlot, setAddingSlot] = useState(false)
  const sessionIdToProfile = useRef<Record<string, string>>({})
  const confirmResolverRef = useRef<((value: boolean) => void) | null>(null)
  const slotsRef = useRef(slots)
  const slotConfigsRef = useRef(slotConfigs)
  slotsRef.current = slots
  slotConfigsRef.current = slotConfigs

  const applyEventToSlot = useCallback((profileId: string, event: SessionEvent) => {
    setSlots((prev) => {
      const slot = prev[profileId] ?? createEmptySlotState()
      let next: SlotState = { ...slot }

      if (event.type === 'Screenshot' && event.screenshotBase64) {
        next = {
          ...next,
          screenshot: `data:image/jpeg;base64,${event.screenshotBase64}`,
        }
      }

      if (event.type === 'Log' && event.message) {
        next = {
          ...next,
          logs: [...next.logs, `[${formatTime(event.timestamp)}] ${event.message}`],
        }
      }

      if (event.type === 'StepChanged') {
        next = {
          ...next,
          session: next.session
            ? {
                ...next.session,
                status: 'Running',
                currentStep: event.currentStep ?? next.session.currentStep,
                totalSteps: event.totalSteps ?? next.session.totalSteps,
                currentStepName: event.stepName ?? next.session.currentStepName,
              }
            : next.session,
        }
        if (event.message) {
          next = {
            ...next,
            logs: [...next.logs, `[${formatTime(event.timestamp)}] ${event.message}`],
          }
        }
      }

      if (event.type === 'StatusChanged' && event.status) {
        next = {
          ...next,
          session: next.session
            ? { ...next.session, status: normalizeStatus(event.status) }
            : next.session,
        }
      }

      if (event.type === 'Completed') {
        next = {
          ...next,
          session: next.session
            ? {
                ...next.session,
                status: next.session.autoRestart ? 'Running' : 'Completed',
                finishedAt: next.session.autoRestart ? undefined : new Date().toISOString(),
              }
            : next.session,
          logs: [
            ...next.logs,
            `[${formatTime(event.timestamp)}] ${event.message ?? 'Сессия остановлена вручную'}`,
          ],
        }
      }

      if (event.type === 'Failed') {
        next = {
          ...next,
          session: next.session
            ? {
                ...next.session,
                status: next.session.autoRestart ? next.session.status : 'Failed',
                errorMessage: event.message,
                finishedAt: next.session.autoRestart ? undefined : new Date().toISOString(),
              }
            : next.session,
        }
        if (event.message) {
          next = {
            ...next,
            logs: [...next.logs, `[${formatTime(event.timestamp)}] ✗ ${event.message}`],
          }
        }
      }

      return { ...prev, [profileId]: next }
    })
  }, [])

  const handleEvent = useCallback(
    (event: SessionEvent) => {
      let profileId = sessionIdToProfile.current[event.sessionId]

      if (!profileId) {
        for (const slot of slotConfigsRef.current) {
          if (slotsRef.current[slot.profileId]?.session?.id === event.sessionId) {
            profileId = slot.profileId
            sessionIdToProfile.current[event.sessionId] = profileId
            break
          }
        }
      }

      if (profileId) applyEventToSlot(profileId, event)
    },
    [applyEventToSlot],
  )

  const syncSlotsWithSessions = useCallback(async () => {
    const configs = await fetchSlots()
    setSlotConfigs(configs)
    setSlots((prev) => {
      const next: Record<string, SlotState> = { ...prev }
      for (const cfg of configs) {
        if (!next[cfg.profileId]) next[cfg.profileId] = createEmptySlotState()
      }
      return next
    })

    const sessions = await fetchSessions()
    setSlots((prev) => {
      const next = { ...prev }
      for (const cfg of configs) {
        const active = sessions.find(
          (s) =>
            s.profileId === cfg.profileId &&
            (s.status === 'Starting' || s.status === 'Running' || s.autoRestart),
        )
        if (active) {
          sessionIdToProfile.current[active.id] = cfg.profileId
          next[cfg.profileId] = {
            ...next[cfg.profileId],
            session: active,
            logs: active.logs.length > 0 ? active.logs : next[cfg.profileId]?.logs ?? [],
          }
        }
      }
      return next
    })
  }, [])

  useEffect(() => {
    createSessionHub(handleEvent)

    const sync = async () => {
      const healthy = await checkApiHealth()
      setApiUp(healthy)
      if (!healthy) {
        setConnected(false)
        return
      }

      try {
        await syncSlotsWithSessions()
      } catch {
        // API may be restarting.
      }

      const hubOk = await ensureHubConnected()
      setConnected(hubOk)
    }

    void sync()
    const interval = window.setInterval(() => void sync(), 3000)

    return () => {
      window.clearInterval(interval)
    }
  }, [handleEvent, syncSlotsWithSessions])

  useEffect(() => {
    const tick = () => {
      setMskTime(
        new Date().toLocaleTimeString('ru-RU', {
          timeZone: 'Europe/Moscow',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
        }),
      )
    }
    tick()
    const id = window.setInterval(tick, 1000)
    return () => window.clearInterval(id)
  }, [])

  const onStart = async (profileId: string) => {
    setSlots((prev) => ({
      ...prev,
      [profileId]: { ...prev[profileId], loading: true },
    }))
    setNotice(null)

    try {
      const session = await startSession(profileId)
      sessionIdToProfile.current[session.id] = profileId
      setSlots((prev) => ({
        ...prev,
        [profileId]: {
          session,
          logs: [`[${formatTime(new Date().toISOString())}] Сессия ${session.id.slice(0, 8)}… запущена`],
          screenshot: null,
          loading: false,
        },
      }))
    } catch (e) {
      setNotice({ kind: 'error', text: e instanceof Error ? e.message : 'Ошибка запуска' })
      setSlots((prev) => ({
        ...prev,
        [profileId]: { ...prev[profileId], loading: false },
      }))
    }
  }

  const onStop = async (profileId: string) => {
    const session = slots[profileId]?.session
    if (!session) return

    setSlots((prev) => ({
      ...prev,
      [profileId]: { ...prev[profileId], loading: true },
    }))

    try {
      await stopSession(session.id)
      delete sessionIdToProfile.current[session.id]
      setSlots((prev) => ({
        ...prev,
        [profileId]: {
          ...prev[profileId],
          session: prev[profileId].session
            ? { ...prev[profileId].session!, status: 'Stopped', autoRestart: false }
            : null,
          loading: false,
        },
      }))
    } catch (e) {
      setNotice({ kind: 'error', text: e instanceof Error ? e.message : 'Ошибка остановки' })
      setSlots((prev) => ({
        ...prev,
        [profileId]: { ...prev[profileId], loading: false },
      }))
    }
  }

  const onAddSlot = async () => {
    setAddingSlot(true)
    setNotice(null)
    try {
      const created = await addSlot(`Сессия ${slotConfigs.length + 1}`)
      setSlotConfigs((prev) => [...prev, created])
      setSlots((prev) => ({ ...prev, [created.profileId]: createEmptySlotState() }))
    } catch (e) {
      setNotice({ kind: 'error', text: e instanceof Error ? e.message : 'Не удалось добавить сессию' })
    } finally {
      setAddingSlot(false)
    }
  }

  const openConfirm = useCallback(
    (title: string, message: string, confirmText = 'Подтвердить', cancelText = 'Отмена') =>
      new Promise<boolean>((resolve) => {
        confirmResolverRef.current = resolve
        setConfirmState({ title, message, confirmText, cancelText })
      }),
    [],
  )

  const closeConfirm = useCallback((accepted: boolean) => {
    setConfirmState(null)
    const resolver = confirmResolverRef.current
    confirmResolverRef.current = null
    resolver?.(accepted)
  }, [])

  const onDeleteSlot = async (profileId: string) => {
    if (slotConfigs.length <= 1) {
      setNotice({ kind: 'error', text: 'Нельзя удалить последнюю сессию' })
      return
    }

    const session = slots[profileId]?.session
    const isActive =
      session && (session.status === 'Starting' || session.status === 'Running')
    const approved = await openConfirm(
      'Удаление сессии',
      isActive ? 'Сессия сейчас запущена. Остановить и удалить слот?' : 'Удалить этот слот?',
      'Удалить',
      'Отмена',
    )
    if (!approved) return

    setNotice(null)
    try {
      if (session) {
        await stopSession(session.id)
        delete sessionIdToProfile.current[session.id]
      }
      await deleteSlot(profileId)
      setSlotConfigs((prev) => prev.filter((s) => s.profileId !== profileId))
      setSlots((prev) => {
        const next = { ...prev }
        delete next[profileId]
        return next
      })
    } catch (e) {
      setNotice({ kind: 'error', text: e instanceof Error ? e.message : 'Не удалось удалить сессию' })
    }
  }

  const onScheduleChange = async (
    profileId: string,
    patch: Partial<Pick<SessionSlotConfig, 'scheduleEnabled' | 'scheduledStartMsk'>>,
  ) => {
    try {
      const updated = await updateSlot(profileId, patch)
      setSlotConfigs((prev) =>
        prev.map((s) => (s.profileId === profileId ? { ...s, ...updated } : s)),
      )
    } catch (e) {
      setNotice({
        kind: 'error',
        text: e instanceof Error ? e.message : 'Не удалось сохранить расписание',
      })
    }
  }

  const activeCount = useMemo(
    () =>
      slotConfigs.filter((s) => {
        const status = normalizeStatus(slots[s.profileId]?.session?.status)
        return status === 'Starting' || status === 'Running'
      }).length,
    [slots, slotConfigs],
  )

  return (
    <div className="app">
      <header className="header">
        <div>
          <h1>AwardsFerm</h1>
          <p className="subtitle">
            {slotConfigs.length} сессий — Яндекс Игры · Slither Worms Wars!
          </p>
        </div>
        <div className="header-meta">
          <span className="badge">МСК {mskTime}</span>
          <span className={`badge ${apiUp ? 'badge-ok' : 'badge-warn'}`}>
            API {apiUp ? 'доступен' : 'недоступен'}
          </span>
          <span className={`badge ${connected ? 'badge-ok' : 'badge-warn'}`}>
            SignalR {connected ? 'подключён' : 'отключён'}
          </span>
          <span className="badge">
            Активно: {activeCount}/{slotConfigs.length}
          </span>
        </div>
      </header>

      <RsyaStatsPanel />

      {notice && (
        <div className={`popup-notice ${notice.kind === 'error' ? 'popup-notice-error' : 'popup-notice-success'}`}>
          <span>{notice.text}</span>
          <button className="popup-notice-close" onClick={() => setNotice(null)}>
            ×
          </button>
        </div>
      )}

      {!apiUp && (
        <div className="error-banner">
          API на порту 8080 не отвечает. Запустите:{' '}
          <code>dotnet run --project src\AwardsFerm.Api\AwardsFerm.Api.csproj</code>
        </div>
      )}

      <div className="slots-toolbar">
        <button
          className="btn btn-secondary btn-sm"
          onClick={() => void onAddSlot()}
          disabled={addingSlot || slotConfigs.length >= 10}
        >
          + Добавить сессию
        </button>
        <span className="slots-hint">
          Автозапуск по расписанию (МСК) — в карточке каждой сессии. Работает при запущенном API.
        </span>
      </div>

      <div className="sessions-grid">
        {slotConfigs.map((slot) => (
          <SessionPanel
            key={slot.profileId}
            config={slot}
            state={slots[slot.profileId] ?? createEmptySlotState()}
            canDelete={slotConfigs.length > 1}
            onStart={() => void onStart(slot.profileId)}
            onStop={() => void onStop(slot.profileId)}
            onDelete={() => void onDeleteSlot(slot.profileId)}
            onScheduleChange={(patch) => void onScheduleChange(slot.profileId, patch)}
          />
        ))}
      </div>

      <footer className="footer">
        <span>Профили: profiles/session-XXX · расписание: profiles/slots.json</span>
        <span>Автоперезапуск после 20 игр или закрытия браузера</span>
      </footer>

      {confirmState && (
        <div className="popup-overlay">
          <div className="popup-dialog">
            <h3>{confirmState.title}</h3>
            <p>{confirmState.message}</p>
            <div className="popup-actions">
              <button className="btn btn-danger btn-sm" onClick={() => closeConfirm(true)}>
                {confirmState.confirmText}
              </button>
              <button className="btn btn-secondary btn-sm" onClick={() => closeConfirm(false)}>
                {confirmState.cancelText}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function SessionPanel({
  config,
  state,
  canDelete,
  onStart,
  onStop,
  onDelete,
  onScheduleChange,
}: {
  config: SessionSlotConfig
  state: SlotState
  canDelete: boolean
  onStart: () => void
  onStop: () => void
  onDelete: () => void
  onScheduleChange: (
    patch: Partial<Pick<SessionSlotConfig, 'scheduleEnabled' | 'scheduledStartMsk'>>,
  ) => void
}) {
  const logViewRef = useRef<HTMLDivElement>(null)
  const sessionStatus = normalizeStatus(state.session?.status)
  const isActive = sessionStatus === 'Starting' || sessionStatus === 'Running'
  const progress =
    state.session && state.session.totalSteps > 0
      ? Math.round((state.session.currentStep / state.session.totalSteps) * 100)
      : 0
  const durationText = formatDuration(state.session?.startedAt, state.session?.finishedAt)

  useEffect(() => {
    const el = logViewRef.current
    if (!el) return
    el.scrollTop = el.scrollHeight
  }, [state.logs])

  return (
    <section className="session-panel">
      <div className="session-panel-header">
        <div>
          <h2>{config.label}</h2>
          <span className="profile-id">{config.profileId}</span>
        </div>
        <div className="session-header-meta">
          <span className={`status-value status-${statusCssClass(state.session?.status)}`}>
            {statusLabels[sessionStatus]}
          </span>
          <span className="session-duration">{durationText}</span>
        </div>
      </div>

      <div className="schedule-row">
        <label className="schedule-label">
          <input
            type="checkbox"
            checked={config.scheduleEnabled}
            onChange={(e) =>
              onScheduleChange({
                scheduleEnabled: e.target.checked,
                scheduledStartMsk: config.scheduledStartMsk ?? '09:00',
              })
            }
          />
          Автозапуск (МСК)
        </label>
        <input
          type="time"
          className="schedule-time"
          value={config.scheduledStartMsk ?? '09:00'}
          disabled={!config.scheduleEnabled}
          onChange={(e) => onScheduleChange({ scheduledStartMsk: e.target.value })}
        />
      </div>

      <div className="session-toolbar">
        <button
          className="btn btn-primary btn-sm"
          onClick={onStart}
          disabled={state.loading || isActive}
        >
          ▶ Старт
        </button>
        <button
          className="btn btn-danger btn-sm"
          onClick={onStop}
          disabled={state.loading || !isActive}
        >
          ■ Стоп
        </button>
        <button
          className="btn btn-secondary btn-sm"
          onClick={onDelete}
          disabled={!canDelete || state.loading}
          title={canDelete ? 'Удалить слот' : 'Нельзя удалить последний слот'}
        >
          ✕
        </button>
        <span className="step-hint">
          {state.session?.currentStep
            ? `Шаг ${state.session.currentStep}/${state.session.totalSteps}`
            : '—'}
        </span>
      </div>

      <div className="progress-bar progress-bar-sm">
        <div className="progress-fill" style={{ width: `${progress}%` }} />
      </div>

      <div className="browser-viewport session-viewport">
        {state.screenshot ? (
          <img src={state.screenshot} alt={`Live ${config.profileId}`} className="screenshot" />
        ) : (
          <div className="screenshot-placeholder">
            {isActive ? 'Ожидание кадра…' : 'Нажмите «Старт»'}
          </div>
        )}
      </div>

      <div ref={logViewRef} className="log-view session-log">
        {state.logs.length === 0 ? (
          <span className="log-empty">Лог пуст</span>
        ) : (
          state.logs.slice(-12).map((line, i) => (
            <div key={i} className="log-line">
              {line}
            </div>
          ))
        )}
      </div>
    </section>
  )
}

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString('ru-RU')
  } catch {
    return '--:--:--'
  }
}

function formatDuration(startedAt?: string, finishedAt?: string): string {
  if (!startedAt) return '00:00:00'
  const startTs = new Date(startedAt).getTime()
  if (Number.isNaN(startTs)) return '00:00:00'
  const endTs = finishedAt ? new Date(finishedAt).getTime() : Date.now()
  const safeEnd = Number.isNaN(endTs) ? Date.now() : endTs
  const totalSeconds = Math.max(0, Math.floor((safeEnd - startTs) / 1000))
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60
  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds
    .toString()
    .padStart(2, '0')}`
}

export default App
