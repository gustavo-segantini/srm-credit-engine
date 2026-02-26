# ── Stage 1: build ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore only — leverages Docker layer cache
COPY ["src/SrmCreditEngine.Domain/SrmCreditEngine.Domain.csproj",          "src/SrmCreditEngine.Domain/"]
COPY ["src/SrmCreditEngine.Application/SrmCreditEngine.Application.csproj","src/SrmCreditEngine.Application/"]
COPY ["src/SrmCreditEngine.Infrastructure/SrmCreditEngine.Infrastructure.csproj","src/SrmCreditEngine.Infrastructure/"]
COPY ["src/SrmCreditEngine.API/SrmCreditEngine.API.csproj",                "src/SrmCreditEngine.API/"]
RUN dotnet restore "src/SrmCreditEngine.API/SrmCreditEngine.API.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/SrmCreditEngine.API"
RUN dotnet publish "SrmCreditEngine.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Stage 2: runtime ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SrmCreditEngine.API.dll"]
