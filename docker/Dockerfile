# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG TARGETARCH
WORKDIR /src

# Copy solution and project definitions for optimal Docker layer caching
COPY Directory.Build.props ./
COPY src/ServiceTemplate.Contracts/ServiceTemplate.Contracts.csproj src/ServiceTemplate.Contracts/
COPY src/ServiceTemplate.Domain/ServiceTemplate.Domain.csproj src/ServiceTemplate.Domain/
COPY src/ServiceTemplate.Application/ServiceTemplate.Application.csproj src/ServiceTemplate.Application/
COPY src/ServiceTemplate.Infrastructure/ServiceTemplate.Infrastructure.csproj src/ServiceTemplate.Infrastructure/
COPY src/ServiceTemplate.Api/ServiceTemplate.Api.csproj src/ServiceTemplate.Api/
COPY ServiceTemplate.sln ./

RUN dotnet restore ServiceTemplate.sln -a $TARGETARCH

# Copy complete source code
COPY src/ ./src/

WORKDIR /src/src/ServiceTemplate.Api
RUN dotnet publish -c Release -a $TARGETARCH -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Hardened Runtime (Ubuntu Chiseled - Non-Root, No Shell, Minimal Attack Surface)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ServiceTemplate.Api.dll"]
