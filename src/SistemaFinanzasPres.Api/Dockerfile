# ── Etapa 1: build ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY SistemaFinanzasPres.Api.csproj ./
RUN dotnet restore SistemaFinanzasPres.Api.csproj

COPY . .
RUN dotnet publish SistemaFinanzasPres.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── Etapa 2: runtime ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaFinanzasPres.Api.dll"]
