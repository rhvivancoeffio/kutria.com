# === src/Commerce.Api/Dockerfile ===
# FIX V3 - Validado
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Central Package Management
COPY Directory.Packages.props ./
COPY data/ ./data/
COPY global.json ./
# Copy csproj for cacheable restore
COPY src/Commerce.Domain/Commerce.Domain.csproj src/Commerce.Domain/
COPY src/Commerce.Application/Commerce.Application.csproj src/Commerce.Application/
COPY src/Commerce.Infrastructure.Persistence/Commerce.Infrastructure.Persistence.csproj src/Commerce.Infrastructure.Persistence/
COPY src/Commerce.Infrastructure/Commerce.Infrastructure.csproj src/Commerce.Infrastructure/
COPY src/Commerce.Api/Commerce.Api.csproj src/Commerce.Api/
RUN dotnet restore src/Commerce.Api/Commerce.Api.csproj
# Copy rest
COPY src/ ./src/
RUN dotnet publish src/Commerce.Api/Commerce.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
# Azure Container Apps escucha en 5000, pero para non-root mejor 8080. Mantenemos 5000 por tu compose actual.
ENV ASPNETCORE_URLS=http://+:5000
# Well-Architected: non-root
USER 1000
ENTRYPOINT ["dotnet", "Commerce.Api.dll"]
