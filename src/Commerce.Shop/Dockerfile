# === src/Commerce.Shop/Dockerfile ===
# FIX: Era 9.0 + Commerce.Shared (que no existe). Ahora 10.0 y deps reales
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY data/ ./data/
COPY global.json ./
# Shop depende de Domain/Application/Infrastructure igual que Api
COPY src/Commerce.Domain/Commerce.Domain.csproj src/Commerce.Domain/
COPY src/Commerce.Application/Commerce.Application.csproj src/Commerce.Application/
COPY src/Commerce.Infrastructure.Persistence/Commerce.Infrastructure.Persistence.csproj src/Commerce.Infrastructure.Persistence/
COPY src/Commerce.Infrastructure/Commerce.Infrastructure.csproj src/Commerce.Infrastructure/
COPY src/Commerce.Shop/Commerce.Shop.csproj src/Commerce.Shop/
RUN dotnet restore src/Commerce.Shop/Commerce.Shop.csproj
COPY src/ ./src/
RUN dotnet publish src/Commerce.Shop/Commerce.Shop.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
USER 1000
ENTRYPOINT ["dotnet", "Commerce.Shop.dll"]
