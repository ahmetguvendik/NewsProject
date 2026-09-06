FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Compose healthcheck'i /health/ready'e istek atıyor; aspnet imajında HTTP istemcisi yok.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/IdentityService/Presentation/IdentityService.WebApi/IdentityService.WebApi.csproj", "src/Services/IdentityService/Presentation/IdentityService.WebApi/"]
COPY ["src/Services/IdentityService/Core/IdentityService.Application/IdentityService.Application.csproj", "src/Services/IdentityService/Core/IdentityService.Application/"]
COPY ["src/Services/IdentityService/Core/IdentityService.Domain/IdentityService.Domain.csproj", "src/Services/IdentityService/Core/IdentityService.Domain/"]
COPY ["src/Services/IdentityService/Infrastructure/IdentityService.Persistance/IdentityService.Persistance.csproj", "src/Services/IdentityService/Infrastructure/IdentityService.Persistance/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/Shared.HealthChecks/Shared.HealthChecks.csproj", "src/Shared.HealthChecks/"]

RUN dotnet restore "src/Services/IdentityService/Presentation/IdentityService.WebApi/IdentityService.WebApi.csproj"

COPY . .

RUN dotnet publish "src/Services/IdentityService/Presentation/IdentityService.WebApi/IdentityService.WebApi.csproj" \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.WebApi.dll"]
