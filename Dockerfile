# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AiDebugger.sln ./
COPY src/AiDebugger.Storage/AiDebugger.Storage.csproj src/AiDebugger.Storage/
COPY src/AiDebugger.Ingest/AiDebugger.Ingest.csproj src/AiDebugger.Ingest/
COPY src/AiDebugger.Packager/AiDebugger.Packager.csproj src/AiDebugger.Packager/
COPY src/AiDebugger.Retrieval/AiDebugger.Retrieval.csproj src/AiDebugger.Retrieval/
COPY src/AiDebugger.Orchestrator/AiDebugger.Orchestrator.csproj src/AiDebugger.Orchestrator/
COPY src/AiDebugger.Publisher/AiDebugger.Publisher.csproj src/AiDebugger.Publisher/
COPY src/AiDebugger.Worker/AiDebugger.Worker.csproj src/AiDebugger.Worker/

COPY src ./src
RUN dotnet restore src/AiDebugger.Worker/AiDebugger.Worker.csproj && \
    dotnet publish src/AiDebugger.Worker/AiDebugger.Worker.csproj -c Release -o /app/publish --no-restore

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AiDebugger.Worker.dll"]
