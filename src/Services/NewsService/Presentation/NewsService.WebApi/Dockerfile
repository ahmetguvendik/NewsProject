FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/NewsService/Presentation/NewsService.WebApi/NewsService.WebApi.csproj", "src/Services/NewsService/Presentation/NewsService.WebApi/"]
COPY ["src/Services/NewsService/Core/NewsService.Application/NewsService.Application.csproj", "src/Services/NewsService/Core/NewsService.Application/"]
COPY ["src/Services/NewsService/Core/NewsService.Domain/NewsService.Domain.csproj", "src/Services/NewsService/Core/NewsService.Domain/"]
COPY ["src/Services/NewsService/Infrastructure/NewsService.Persistance/NewsService.Persistance.csproj", "src/Services/NewsService/Infrastructure/NewsService.Persistance/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/Services/NewsService/Presentation/NewsService.WebApi/NewsService.WebApi.csproj"

COPY . .

RUN dotnet publish "src/Services/NewsService/Presentation/NewsService.WebApi/NewsService.WebApi.csproj" \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NewsService.WebApi.dll"]
