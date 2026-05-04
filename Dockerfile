# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first for better layer caching.
COPY Dashboard.Web/Dashboard.Web.csproj Dashboard.Web/
RUN dotnet restore Dashboard.Web/Dashboard.Web.csproj

COPY . .
RUN dotnet publish Dashboard.Web/Dashboard.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dashboard.Web.dll"]
