# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MyApp.Domain/MyApp.Domain.csproj", "MyApp.Domain/"]
COPY ["MyApp.Application/MyApp.Application.csproj", "MyApp.Application/"]
COPY ["MyApp.Infrastructure/MyApp.Infrastructure.csproj", "MyApp.Infrastructure/"]
COPY ["MyApp.API/MyApp.API.csproj", "MyApp.API/"]

RUN dotnet restore "MyApp.API/MyApp.API.csproj"

COPY . .
RUN dotnet build "MyApp.API/MyApp.API.csproj" -c Release -o /app/build

# ── Publish stage ─────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "MyApp.API/MyApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Developpement

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MyApp.API.dll"]
