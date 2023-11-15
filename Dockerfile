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
ENTRYPOINT ["dotnet", "SDMetaTool.dll"]