FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Compose healthcheck'i /health/ready'e istek atıyor; aspnet imajında HTTP istemcisi yok.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/NotificationService/Presentation/NotificationService.WebApi/NotificationService.WebApi.csproj", "src/Services/NotificationService/Presentation/NotificationService.WebApi/"]
COPY ["src/Services/NotificationService/Infrastructure/NotificationService.Persistance/NotificationService.Persistance.csproj", "src/Services/NotificationService/Infrastructure/NotificationService.Persistance/"]
COPY ["src/Services/NotificationService/Core/NotificationService.Application/NotificationService.Application.csproj", "src/Services/NotificationService/Core/NotificationService.Application/"]
COPY ["src/Services/NotificationService/Core/NotificationService.Domain/NotificationService.Domain.csproj", "src/Services/NotificationService/Core/NotificationService.Domain/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/Shared.HealthChecks/Shared.HealthChecks.csproj", "src/Shared.HealthChecks/"]

RUN dotnet restore "src/Services/NotificationService/Presentation/NotificationService.WebApi/NotificationService.WebApi.csproj"

COPY . .
WORKDIR "/src/src/Services/NotificationService/Presentation/NotificationService.WebApi"
RUN dotnet publish "NotificationService.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.WebApi.dll"]
