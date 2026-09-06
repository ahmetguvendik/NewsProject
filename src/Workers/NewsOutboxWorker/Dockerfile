FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Worker artık sağlık ucu açıyor; compose healthcheck'i buraya istek atıyor.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Workers/NewsOutboxWorker/NewsOutboxWorker.csproj", "src/Workers/NewsOutboxWorker/"]
COPY ["src/Services/NewsService/Core/NewsService.Domain/NewsService.Domain.csproj", "src/Services/NewsService/Core/NewsService.Domain/"]

COPY ["src/HealthCheck.Registration/HealthCheck.Registration.csproj", "src/HealthCheck.Registration/"]

RUN dotnet restore "src/Workers/NewsOutboxWorker/NewsOutboxWorker.csproj"

COPY . .
WORKDIR "/src/src/Workers/NewsOutboxWorker"
RUN dotnet publish "NewsOutboxWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NewsOutboxWorker.dll"]
