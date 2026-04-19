FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Workers/NewsOutboxWorker/NewsOutboxWorker.csproj", "src/Workers/NewsOutboxWorker/"]
COPY ["src/Services/NewsService/Core/NewsService.Domain/NewsService.Domain.csproj", "src/Services/NewsService/Core/NewsService.Domain/"]

RUN dotnet restore "src/Workers/NewsOutboxWorker/NewsOutboxWorker.csproj"

COPY . .
WORKDIR "/src/src/Workers/NewsOutboxWorker"
RUN dotnet publish "NewsOutboxWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NewsOutboxWorker.dll"]
