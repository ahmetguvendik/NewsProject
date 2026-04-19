FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Workers/NotificationInboxWorker/NotificationInboxWorker.csproj", "src/Workers/NotificationInboxWorker/"]
COPY ["src/Services/NotificationService/Core/NotificationService.Domain/NotificationService.Domain.csproj", "src/Services/NotificationService/Core/NotificationService.Domain/"]
COPY ["src/Services/NotificationService/Core/NotificationService.Application/NotificationService.Application.csproj", "src/Services/NotificationService/Core/NotificationService.Application/"]
COPY ["src/Services/NotificationService/Infrastructure/NotificationService.Persistance/NotificationService.Persistance.csproj", "src/Services/NotificationService/Infrastructure/NotificationService.Persistance/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/Workers/NotificationInboxWorker/NotificationInboxWorker.csproj"

COPY . .
WORKDIR "/src/src/Workers/NotificationInboxWorker"
RUN dotnet publish "NotificationInboxWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationInboxWorker.dll"]
