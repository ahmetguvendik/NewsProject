FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Workers/IdentityOutboxWorker/IdentityOutboxWorker.csproj", "src/Workers/IdentityOutboxWorker/"]
COPY ["src/Services/IdentityService/Core/IdentityService.Domain/IdentityService.Domain.csproj", "src/Services/IdentityService/Core/IdentityService.Domain/"]

RUN dotnet restore "src/Workers/IdentityOutboxWorker/IdentityOutboxWorker.csproj"

COPY . .
WORKDIR "/src/src/Workers/IdentityOutboxWorker"
RUN dotnet publish "IdentityOutboxWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityOutboxWorker.dll"]
