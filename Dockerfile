FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ServiceDesk.Domain/ServiceDesk.Domain.csproj", "src/ServiceDesk.Domain/"]
COPY ["src/ServiceDesk.Infrastructure/ServiceDesk.Infrastructure.csproj", "src/ServiceDesk.Infrastructure/"]
COPY ["src/ServiceDesk.Web/ServiceDesk.Web.csproj", "src/ServiceDesk.Web/"]
RUN dotnet restore "src/ServiceDesk.Web/ServiceDesk.Web.csproj"

COPY . .
WORKDIR /src/src/ServiceDesk.Web
RUN dotnet publish "ServiceDesk.Web.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet ServiceDesk.Web.dll --urls http://0.0.0.0:${PORT:-10000}"]
