FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish ./SDMetaTool -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "SDMetaTool.dll"]