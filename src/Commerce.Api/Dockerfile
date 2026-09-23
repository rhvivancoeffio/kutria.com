FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props ./
COPY global.json ./
COPY src/Commerce.Domain/Commerce.Domain.csproj src/Commerce.Domain/
COPY src/Commerce.Application/Commerce.Application.csproj src/Commerce.Application/
COPY src/Commerce.Infrastructure.Persistence/Commerce.Infrastructure.Persistence.csproj src/Commerce.Infrastructure.Persistence/
COPY src/Commerce.Infrastructure/Commerce.Infrastructure.csproj src/Commerce.Infrastructure/
COPY src/Commerce.Api/Commerce.Api.csproj src/Commerce.Api/
RUN dotnet restore src/Commerce.Api/Commerce.Api.csproj
COPY src/ ./src/
COPY data/ ./data/
RUN dotnet publish src/Commerce.Api/Commerce.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Commerce.Api.dll"]
