import * as signalR from '@microsoft/signalr'
import type { RsyaDashboard, SessionEvent, SessionInfo, SessionSlotConfig } from './types'
import { normalizeEventType, normalizeSession, normalizeStatus } from './utils/session'

// Пустой VITE_API_URL = same-origin (в dev Vite проксирует /api и /hubs на :8080).
const apiBase = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

function apiPath(path: string): string {
  const normalized = path.startsWith('/') ? path : `/${path}`
  return apiBase ? `${apiBase}${normalized}` : normalized
}

const defaultOptions = {
  searchQuery: 'червячки',
  targetGameTitle: 'Slither Worms Wars!',
  targetGameUrlPart: 'slither-worms-wars-511328',
  playDurationMinSeconds: 120,
  playDurationMaxSeconds: 180,
  headless: false,
}

export async function checkApiHealth(): Promise<boolean> {
  try {
    const res = await fetch(apiPath('/api/health'), { signal: AbortSignal.timeout(3000) })
    return res.ok
  } catch {
    return false
  }
}

export async function fetchSessions(): Promise<SessionInfo[]> {
  const res = await fetch(apiPath('/api/sessions'))
  if (!res.ok) throw new Error('Не удалось получить список сессий')
  const data = (await res.json()) as SessionInfo[]
  return data.map(normalizeSession)
}

export async function fetchCurrentSession(): Promise<SessionInfo | null> {
  const res = await fetch(apiPath('/api/sessions/current'))
  if (!res.ok) throw new Error('Не удалось получить сессию')
  const data = (await res.json()) as SessionInfo | null
  return data ? normalizeSession(data) : null
}

export async function startSession(profileId: string): Promise<SessionInfo> {
  const res = await fetch(apiPath('/api/sessions/start'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      profileId,
      autoRestart: true,
      options: defaultOptions,
    }),
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || 'Не удалось запустить сессию')
  }
  const data = (await res.json()) as SessionInfo
  return normalizeSession(data)
}

export async function stopSession(sessionId: string): Promise<void> {
  const res = await fetch(apiPath(`/api/sessions/${sessionId}/stop`), { method: 'POST' })
  if (!res.ok) throw new Error('Не удалось остановить сессию')
}

export async function fetchSlots(): Promise<SessionSlotConfig[]> {
  const res = await fetch(apiPath('/api/slots'))
  if (!res.ok) throw new Error('Не удалось загрузить слоты сессий')
  return (await res.json()) as SessionSlotConfig[]
}

export async function addSlot(label?: string): Promise<SessionSlotConfig> {
  const res = await fetch(apiPath('/api/slots'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ label }),
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || 'Не удалось добавить сессию')
  }
  return (await res.json()) as SessionSlotConfig
}

export async function updateSlot(
  profileId: string,
  patch: Partial<Pick<SessionSlotConfig, 'label' | 'scheduleEnabled' | 'scheduledStartMsk'>>,
): Promise<SessionSlotConfig> {
  const res = await fetch(apiPath(`/api/slots/${profileId}`), {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(patch),
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || 'Не удалось обновить сессию')
  }
  return (await res.json()) as SessionSlotConfig
}

export async function deleteSlot(profileId: string): Promise<void> {
  const res = await fetch(apiPath(`/api/slots/${profileId}`), { method: 'DELETE' })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || 'Не удалось удалить сессию')
  }
}

export async function fetchRsyaDashboard(): Promise<RsyaDashboard> {
  const res = await fetch(apiPath('/api/rsya/dashboard'))
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || 'Не удалось загрузить статистику РСЯ')
  }
  return (await res.json()) as RsyaDashboard
}

let hubConnection: signalR.HubConnection | null = null
let hubHandler: ((event: SessionEvent) => void) | null = null

function getHubConnection(): signalR.HubConnection {
  if (!hubConnection) {
    const hubUrl = apiPath('/hubs/session')
    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build()
  }
  return hubConnection
}

export function createSessionHub(onEvent: (event: SessionEvent) => void): signalR.HubConnection {
  const hub = getHubConnection()
  hubHandler = onEvent

  hub.off('SessionEvent')
  hub.on('SessionEvent', (raw: SessionEvent) => {
    onEvent({
      ...raw,
      type: normalizeEventType(raw.type),
      status: raw.status !== undefined ? normalizeStatus(raw.status) : undefined,
    })
  })

  return hub
}

export async function ensureHubConnected(): Promise<boolean> {
  const apiUp = await checkApiHealth()
  if (!apiUp) return false

  const hub = getHubConnection()
  if (hubHandler) {
    hub.off('SessionEvent')
    hub.on('SessionEvent', (raw: SessionEvent) => {
      hubHandler?.({
        ...raw,
        type: normalizeEventType(raw.type),
        status: raw.status !== undefined ? normalizeStatus(raw.status) : undefined,
      })
    })
  }

  if (hub.state === signalR.HubConnectionState.Disconnected) {
    try {
      await hub.start()
    } catch {
      return false
    }
  }

  return hub.state === signalR.HubConnectionState.Connected
}
