FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/HealthDashboard/HealthDashboard.csproj", "src/HealthDashboard/"]

RUN dotnet restore "src/HealthDashboard/HealthDashboard.csproj"

COPY . .

RUN dotnet publish "src/HealthDashboard/HealthDashboard.csproj" \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthDashboard.dll"]
