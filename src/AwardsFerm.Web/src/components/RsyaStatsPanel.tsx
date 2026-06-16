import { useEffect, useState } from 'react'
import { fetchRsyaDashboard } from '../api'
import type { RsyaDashboard } from '../types'

const REFRESH_MS = 120_000

export function RsyaStatsPanel() {
  const [data, setData] = useState<RsyaDashboard | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      try {
        const dashboard = await fetchRsyaDashboard()
        if (!cancelled) setData(dashboard)
      } catch (e) {
        if (!cancelled) {
          setData({
            configured: false,
            currency: 'RUB',
            today: emptyStats(),
            yesterday: emptyStats(),
            thisMonth: emptyStats(),
            dailyChart: [],
            updatedAt: new Date().toISOString(),
            error: e instanceof Error ? e.message : 'Ошибка загрузки РСЯ',
          })
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()
    const interval = window.setInterval(() => void load(), REFRESH_MS)
    return () => {
      cancelled = true
      window.clearInterval(interval)
    }
  }, [])

  if (loading && !data) {
    return (
      <section className="rsya-panel">
        <div className="rsya-panel-header">
          <h2>РСЯ — статистика</h2>
          <span className="rsya-muted">Загрузка…</span>
        </div>
      </section>
    )
  }

  if (!data?.configured) {
    return (
      <section className="rsya-panel rsya-panel-warn">
        <div className="rsya-panel-header">
          <h2>РСЯ — статистика</h2>
        </div>
        <p className="rsya-hint">
          {data?.error ??
            'Токен не настроен. Создайте файл profiles/rsya-token.txt или укажите YandexRsya:OAuthToken в appsettings.'}
        </p>
      </section>
    )
  }

  const currency = data.currency || 'RUB'

  return (
    <section className="rsya-panel">
      <div className="rsya-panel-header">
        <div>
          <h2>РСЯ — статистика</h2>
          {data.reportTitle && <p className="rsya-subtitle">{data.reportTitle}</p>}
        </div>
        <div className="rsya-header-meta">
          <span className="badge">Обновлено: {formatDateTime(data.updatedAt)}</span>
          <button className="btn btn-ghost btn-sm" onClick={() => void reload(setData, setLoading)}>
            ↻
          </button>
        </div>
      </div>

      {data.error && <div className="rsya-error">{data.error}</div>}

      <div className="rsya-cards">
        <StatCard
          label="Вознаграждение сегодня"
          value={formatMoney(data.today.reward, currency)}
          accent
        />
        <StatCard label="Вознаграждение вчера" value={formatMoney(data.yesterday.reward, currency)} />
        <StatCard label="Вознаграждение за месяц" value={formatMoney(data.thisMonth.reward, currency)} />
        <StatCard label="Показы сегодня" value={formatInt(data.today.shows)} />
        <StatCard label="Клики сегодня" value={formatInt(data.today.clicks)} />
        <StatCard
          label="Fill rate"
          value={data.today.fillRate != null ? `${data.today.fillRate.toFixed(1)}%` : '—'}
        />
      </div>

      {data.dailyChart.length > 0 && (
        <div className="rsya-chart">
          <h3>Вознаграждение по дням (14 дней)</h3>
          <div className="rsya-chart-bars">
            {data.dailyChart.map((point) => {
              const max = Math.max(...data.dailyChart.map((p) => p.reward), 0.01)
              const height = Math.max(4, (point.reward / max) * 100)
              return (
                <div key={point.date} className="rsya-bar-col" title={`${point.date}: ${formatMoney(point.reward, currency)}`}>
                  <div className="rsya-bar" style={{ height: `${height}%` }} />
                  <span className="rsya-bar-label">{formatShortDate(point.date)}</span>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </section>
  )
}

function StatCard({ label, value, accent }: { label: string; value: string; accent?: boolean }) {
  return (
    <div className={`rsya-card${accent ? ' rsya-card-accent' : ''}`}>
      <span className="rsya-card-label">{label}</span>
      <span className="rsya-card-value">{value}</span>
    </div>
  )
}

async function reload(
  setData: (d: RsyaDashboard) => void,
  setLoading: (v: boolean) => void,
) {
  setLoading(true)
  try {
    setData(await fetchRsyaDashboard())
  } finally {
    setLoading(false)
  }
}

function emptyStats() {
  return { reward: 0, shows: 0, clicks: 0, hits: 0 }
}

function formatMoney(value: number, currency: string): string {
  try {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency,
      maximumFractionDigits: 2,
    }).format(value)
  } catch {
    return `${value.toFixed(2)} ${currency}`
  }
}

function formatInt(value: number): string {
  return new Intl.NumberFormat('ru-RU').format(value)
}

function formatDateTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch {
    return '—'
  }
}

function formatShortDate(iso: string): string {
  try {
    const d = new Date(iso)
    return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' })
  } catch {
    return iso.slice(5)
  }
}
