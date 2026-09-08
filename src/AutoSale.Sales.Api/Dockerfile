FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/AutoSale.Sales.Api/AutoSale.Sales.Api.csproj", "src/AutoSale.Sales.Api/"]
COPY ["src/AutoSale.Sales.Application/AutoSale.Sales.Application.csproj", "src/AutoSale.Sales.Application/"]
COPY ["src/AutoSale.Sales.Domain/AutoSale.Sales.Domain.csproj", "src/AutoSale.Sales.Domain/"]
COPY ["src/AutoSale.Sales.Infrastructure/AutoSale.Sales.Infrastructure.csproj", "src/AutoSale.Sales.Infrastructure/"]
COPY ["src/BuildingBlocks/AutoSale.SharedKernel/AutoSale.SharedKernel.csproj", "src/BuildingBlocks/AutoSale.SharedKernel/"]
RUN dotnet restore "src/AutoSale.Sales.Api/AutoSale.Sales.Api.csproj"

COPY . .
RUN dotnet publish "src/AutoSale.Sales.Api/AutoSale.Sales.Api.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AutoSale.Sales.Api.dll"]
