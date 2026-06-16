# Деплой AwardsFerm

Два варианта публикации (по образцу [bebochka](https://github.com/BlackSmileTeam/bebochka)):

## 1. Монорепо — один `docker compose` (рекомендуется для старта)

Workflow: `.github/workflows/deploy-production.yml`

На сервере:

```bash
docker compose -f docker-compose.production.yml --env-file .env.production up -d --build
```

Порты по умолчанию:

| Сервис   | Хост   | Контейнер |
|----------|--------|-----------|
| Frontend | 55502  | 80        |
| API      | 55501  | 8080      |
| Worker   | —      | 8081      |

Профили браузера: `./profiles` → `/app/profiles` в Worker.

### GitHub Secrets

| Secret | Описание |
|--------|----------|
| `PROD_HOST` | IP или hostname VPS |
| `PROD_USER` | SSH-пользователь |
| `PROD_SSH_PRIVATE_KEY` | Приватный ключ |
| `PROD_SSH_PORT` | SSH-порт |
| `PROD_DEPLOY_PATH` | Каталог на сервере (по умолчанию `/opt/awardsferm`) |
| `PROD_FRONTEND_PORT` | Порт фронта (по умолчанию 55502) |
| `PROD_API_PORT` | Порт API (по умолчанию 55501) |
| `RSYA_OAUTH_TOKEN` | OAuth-токен РСЯ (опционально) |

Скопируйте `.env.production.example` → `.env.production` и отредактируйте при ручном деплое.

## 2. Раздельные контейнеры (как backend + frontend в bebochka)

Workflows в монорепозитории запускаются из **корня** `.github/workflows/`:

- `deploy-production.yml` — полный стек (compose)
- `deploy-api.yml` — только API
- `deploy-worker.yml` — только Worker
- `deploy-frontend.yml` — только UI

Копии в `backend/*/`, `frontend/` — шаблоны на случай выноса в отдельные репозитории (как submodules в bebochka).

Docker-сеть: `awardsferm-edge`. Порядок деплоя: **Worker → API → Frontend**.

### Системный nginx (опционально)

Проксируйте HTTPS на `127.0.0.1:55502` (фронт). API снаружи не обязателен — запросы идут через nginx фронта.

## Локальная разработка

```powershell
.\start-dev.ps1
```

или `docker compose up --build` (dev-compose, порт UI 3000).

## Структура

```
backend/
  AwardsFerm.Api/     Dockerfile + CI
  AwardsFerm.Worker/  Dockerfile + CI
frontend/             Dockerfile, nginx.*.conf + CI
docker/               legacy dev Dockerfiles
docker-compose.yml              dev
docker-compose.production.yml   production
```
