FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer - only project files, so code edits don't invalidate it
COPY global.json Directory.Build.props ./
COPY backend/src/ProductManagement.Domain/ProductManagement.Domain.csproj backend/src/ProductManagement.Domain/
COPY backend/src/ProductManagement.Application/ProductManagement.Application.csproj backend/src/ProductManagement.Application/
COPY backend/src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj backend/src/ProductManagement.Infrastructure/
COPY backend/src/ProductManagement.Api/ProductManagement.Api.csproj backend/src/ProductManagement.Api/
RUN dotnet restore backend/src/ProductManagement.Api/ProductManagement.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/ProductManagement.Api/ProductManagement.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl only for the compose healthcheck
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "ProductManagement.Api.dll"]