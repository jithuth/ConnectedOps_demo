# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=true

# Copy project files for restore caching
COPY ["src/ConnectedOps.Domain/ConnectedOps.Domain.csproj", "src/ConnectedOps.Domain/"]
COPY ["src/ConnectedOps.Application/ConnectedOps.Application.csproj", "src/ConnectedOps.Application/"]
COPY ["src/ConnectedOps.Infrastructure/ConnectedOps.Infrastructure.csproj", "src/ConnectedOps.Infrastructure/"]
COPY ["src/ConnectedOps.Api/ConnectedOps.Api.csproj", "src/ConnectedOps.Api/"]

RUN dotnet restore "src/ConnectedOps.Api/ConnectedOps.Api.csproj"

# Copy all source files
COPY . .

WORKDIR "/src/src/ConnectedOps.Api"
RUN dotnet publish "ConnectedOps.Api.csproj" -c Release -o /app/publish --no-restore -m:1 /p:UseSharedCompilation=false /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ConnectedOps.Api.dll"]
