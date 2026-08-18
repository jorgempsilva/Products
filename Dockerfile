# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage layer caching for restore
COPY src/Products.Domain/Products.Domain.csproj src/Products.Domain/
COPY src/Products.Application/Products.Application.csproj src/Products.Application/
COPY src/Products.Infrastructure/Products.Infrastructure.csproj src/Products.Infrastructure/
COPY src/Products.Api/Products.Api.csproj src/Products.Api/
RUN dotnet restore src/Products.Api/Products.Api.csproj

# Copy the remaining sources and publish
COPY src/ src/
RUN dotnet publish src/Products.Api/Products.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Test stage (used only by the compose "test" profile) ---
FROM build AS test
COPY tests/Products.UnitTests/Products.UnitTests.csproj tests/Products.UnitTests/
COPY tests/Products.IntegrationTests/Products.IntegrationTests.csproj tests/Products.IntegrationTests/
RUN dotnet restore tests/Products.UnitTests/Products.UnitTests.csproj \
    && dotnet restore tests/Products.IntegrationTests/Products.IntegrationTests.csproj
COPY tests/ tests/
ENTRYPOINT ["sh", "-c", "dotnet test tests/Products.UnitTests --nologo && dotnet test tests/Products.IntegrationTests --nologo"]

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root user built into the .NET images (security hardening)
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Products.Api.dll"]
