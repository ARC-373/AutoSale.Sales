FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/AutoSale.Payments.Mock/AutoSale.Payments.Mock.csproj", "src/AutoSale.Payments.Mock/"]
RUN dotnet restore "src/AutoSale.Payments.Mock/AutoSale.Payments.Mock.csproj"
COPY . .
RUN dotnet publish "src/AutoSale.Payments.Mock/AutoSale.Payments.Mock.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM base AS final

RUN apk add --no-cache \
    icu-libs \
    icu-data-full

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /data \
	&& chown -R $APP_UID:$APP_UID /data

USER $APP_UID
ENTRYPOINT ["dotnet", "AutoSale.Payments.Mock.dll"]
