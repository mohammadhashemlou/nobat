# Backend Dockerfile - Development image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

# Expose API port
EXPOSE 5000

# Set environment
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development

# Copy solution and project files
COPY . .

# Restore dependencies
RUN dotnet restore

# Set entry point
ENTRYPOINT ["dotnet", "run", "--project", "src/Nobat.Api", "--urls", "http://0.0.0.0:5000"]
