FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/HealthCheck.Api/HealthCheck.Api.csproj", "src/HealthCheck.Api/"]

RUN dotnet restore "src/HealthCheck.Api/HealthCheck.Api.csproj"

COPY . .

RUN dotnet publish "src/HealthCheck.Api/HealthCheck.Api.csproj" \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthCheck.Api.dll"]
