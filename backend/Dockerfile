# syntax=docker/dockerfile:1
# Production image: multi-stage publish onto the ASP.NET Core runtime, running as non-root.

# --------------------------------------------------------------------------- build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so `restore` is cached independently of source changes.
COPY Nobat.slnx ./
COPY src/Nobat.Domain/Nobat.Domain.csproj src/Nobat.Domain/
COPY src/Nobat.Application/Nobat.Application.csproj src/Nobat.Application/
COPY src/Nobat.Infrastructure/Nobat.Infrastructure.csproj src/Nobat.Infrastructure/
COPY src/Nobat.Api/Nobat.Api.csproj src/Nobat.Api/
RUN dotnet restore src/Nobat.Api/Nobat.Api.csproj

COPY src/ src/
RUN dotnet publish src/Nobat.Api/Nobat.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# --------------------------------------------------------------------------- runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is used by the container health check; the runtime image ships without it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true

# Run as an unprivileged user. The .NET runtime images are Ubuntu-based, which ships
# `useradd` but not Debian's `adduser`.
RUN useradd --uid 10001 --user-group --no-create-home --shell /usr/sbin/nologin appuser
COPY --from=build /app/publish ./
USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Nobat.Api.dll"]
