FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MadVRSEngine.sln ./
COPY src/VendorRisk.Api/VendorRisk.Api.csproj src/VendorRisk.Api/
COPY src/VendorRisk.Infrastructure/VendorRisk.Infrastructure.csproj src/VendorRisk.Infrastructure/
COPY src/VendorRisk.Domain/VendorRisk.Domain.csproj src/VendorRisk.Domain/
RUN dotnet restore "MadVRSEngine.sln"
COPY . .
RUN dotnet publish "src/VendorRisk.Api/VendorRisk.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VendorRisk.Api.dll"]
