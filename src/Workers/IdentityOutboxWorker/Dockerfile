FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Worker artık sağlık ucu açıyor; compose healthcheck'i buraya istek atıyor.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Workers/IdentityOutboxWorker/IdentityOutboxWorker.csproj", "src/Workers/IdentityOutboxWorker/"]
COPY ["src/Services/IdentityService/Core/IdentityService.Domain/IdentityService.Domain.csproj", "src/Services/IdentityService/Core/IdentityService.Domain/"]

COPY ["src/HealthCheck.Registration/HealthCheck.Registration.csproj", "src/HealthCheck.Registration/"]
COPY ["src/Logging.Registration/Logging.Registration.csproj", "src/Logging.Registration/"]

RUN dotnet restore "src/Workers/IdentityOutboxWorker/IdentityOutboxWorker.csproj"

COPY . .
WORKDIR "/src/src/Workers/IdentityOutboxWorker"
RUN dotnet publish "IdentityOutboxWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityOutboxWorker.dll"]
