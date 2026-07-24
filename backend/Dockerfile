# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for Docker layer caching on restore
COPY src/PawTrack.API/PawTrack.API.csproj                       src/PawTrack.API/
COPY src/PawTrack.Application/PawTrack.Application.csproj       src/PawTrack.Application/
COPY src/PawTrack.Domain/PawTrack.Domain.csproj                 src/PawTrack.Domain/
COPY src/PawTrack.Infrastructure/PawTrack.Infrastructure.csproj src/PawTrack.Infrastructure/

RUN dotnet restore src/PawTrack.API/PawTrack.API.csproj

# Copy full source and publish
COPY src/ src/
RUN dotnet publish src/PawTrack.API/PawTrack.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

EXPOSE 8080

# Container Apps requires port 8080; ASPNETCORE_URLS is overridable via env var
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PawTrack.API.dll"]
