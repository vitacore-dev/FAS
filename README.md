# AI Debugger

Сервис автоматического анализа логов (Loki), поиска первопричины с помощью LLM и публикации результатов (GitHub Issues) по архитектуре [AI_DEBUGGER_ARCHITECTURE.md](../AI_DEBUGGER_ARCHITECTURE.md).

## Требования

- .NET 8 SDK
- PostgreSQL
- (опционально) Loki с логами приложений
- (опционально) OpenAI API Key для LLM-анализа
- (опционально) GitHub token для создания issues

## Быстрый старт

1. Клонировать/открыть решение:
   ```bash
   cd ai-debugger
   dotnet restore
   ```

2. Запустить PostgreSQL и создать БД:
   ```bash
   createdb aidebugger
   ```

3. Задать переменные (или отредактировать `src/AiDebugger.Worker/appsettings.json`):
   ```bash
   export ConnectionStrings__Default="Host=localhost;Database=aidebugger;Username=postgres;Password=postgres"
   export Loki__Url="http://localhost:3100"
   export Git__RepoPath="/path/to/your/repo"
   export LLM__ApiKey="sk-..."
   export Publisher__RepoOwner="your-org"
   export Publisher__RepoName="your-repo"
   export Publisher__Token="ghp_..."
   ```

4. Запустить воркер:
   ```bash
   dotnet run --project src/AiDebugger.Worker
   ```

При первом запуске создаётся схема БД и запись в `LokiQueries` (LogQL по умолчанию: `{job=~\".+\"} |= \"Exception\"`).

## Структура решения

- **AiDebugger.Storage** — сущности и DbContext (Postgres).
- **AiDebugger.Ingest** — чтение логов из Loki, checkpoints.
- **AiDebugger.Packager** — fingerprint, Evidence Bundle.
- **AiDebugger.Retrieval** — поиск по репо (`search_repo`, `fetch_snippet`).
- **AiDebugger.Orchestrator** — двухфазный LLM-анализ (log analysis + code grounding).
- **AiDebugger.Publisher** — создание/обновление GitHub issues.
- **AiDebugger.Worker** — хост, планировщик, пайплайн (Ingest → Packager → Orchestrator → Publisher).

## Конфигурация

| Переменная / ключ | Описание |
|-------------------|----------|
| `ConnectionStrings__Default` | Строка подключения Postgres |
| `Loki__Url` | URL Loki API |
| `Loki__ApiKey` | Опциональный API ключ |
| `Git__RepoPath` | Путь к клонированному репозиторию для retrieval |
| `LLM__Provider` | Пока не используется (всегда OpenAI-совместимый API) |
| `LLM__Model` | Модель (например gpt-4o-mini) |
| `LLM__ApiKey` | API ключ LLM |
| `LLM__BaseUrl` | Базовый URL (по умолчанию https://api.openai.com) |
| `Publisher__RepoOwner` / `Publisher__RepoName` | Репозиторий для создания issues |
| `Publisher__Token` | GitHub token |
| `Worker__IngestIntervalMinutes` | Интервал запуска пайплайна (минуты) |
| `Worker__WatermarkSeconds` | Watermark для Loki (секунды) |

## Запуск в Docker

Из каталога `ai-debugger`:

```bash
docker compose up -d
```

Поднимаются: Postgres, Loki, Worker. Для доступа к Loki с хоста: `http://localhost:3100`.

Сборка образа вручную:

```bash
docker compose build aidebugger
```

Переменные окружения (секреты) можно задать в `.env` рядом с `docker-compose.yml` или в `environment` в compose. Пример `.env`:

```
LLM__ApiKey=sk-...
Publisher__RepoOwner=your-org
Publisher__RepoName=your-repo
Publisher__Token=ghp_...
```

Для доступа к репозиторию на хосте смонтируйте каталог в сервис `aidebugger`:

```yaml
volumes:
  - /path/to/your/repo:/repo:ro
environment:
  Git__RepoPath: /repo
```

## Health check

Проверка готовности: приложение запущено и выполняет цикл по расписанию. Эндпоинты `/live` и `/ready` можно добавить позже (ASP.NET Core или отдельный мини-сервер).
