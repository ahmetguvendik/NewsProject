FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Compose healthcheck'i /health/ready'e istek atıyor; aspnet imajında HTTP istemcisi yok.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/NewsService/Presentation/NewsService.WebApi/NewsService.WebApi.csproj", "src/Services/NewsService/Presentation/NewsService.WebApi/"]
COPY ["src/Services/NewsService/Core/NewsService.Application/NewsService.Application.csproj", "src/Services/NewsService/Core/NewsService.Application/"]
COPY ["src/Services/NewsService/Core/NewsService.Domain/NewsService.Domain.csproj", "src/Services/NewsService/Core/NewsService.Domain/"]
COPY ["src/Services/NewsService/Infrastructure/NewsService.Persistance/NewsService.Persistance.csproj", "src/Services/NewsService/Infrastructure/NewsService.Persistance/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/HealthCheck.Registration/HealthCheck.Registration.csproj", "src/HealthCheck.Registration/"]
COPY ["src/Logging.Registration/Logging.Registration.csproj", "src/Logging.Registration/"]

RUN dotnet restore "src/Services/NewsService/Presentation/NewsService.WebApi/NewsService.WebApi.csproj"

COPY . .

RUN dotnet publish "src/Services/NewsService/Presentation/NewsService.WebApi/NewsService.WebApi.csproj" \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NewsService.WebApi.dll"]
