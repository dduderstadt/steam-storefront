FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src                                                       

COPY SteamStorefront.slnx ./
COPY SteamStorefront/SteamStorefront.csproj SteamStorefront/       
COPY Tests/SteamStorefront.Tests.csproj Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish SteamStorefront/SteamStorefront.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SteamStorefront.dll"]