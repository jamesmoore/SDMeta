FROM mcr.microsoft.com/dotnet/sdk:latest AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish ./SDMetaUI -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:latest

WORKDIR /app
COPY --from=build-env /app/out .
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 CMD curl -f http://localhost:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "SDMetaUI.dll"]