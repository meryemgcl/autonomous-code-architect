# Stage 1: Build & Publish stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy proto definitions
COPY shared/protos ./shared/protos

# Copy .NET solution and project files for caching layer
COPY services/dotnet-agent-engine/ArchitectEngine.sln ./services/dotnet-agent-engine/
COPY services/dotnet-agent-engine/src/*/*.csproj ./services/dotnet-agent-engine/src/
COPY services/dotnet-agent-engine/tests/*/*.csproj ./services/dotnet-agent-engine/tests/

# Reorganize projects into respective directories
RUN for file in ./services/dotnet-agent-engine/src/*.csproj; do \
      dirname=$(basename "$file" .csproj); \
      mkdir -p "./services/dotnet-agent-engine/src/$dirname"; \
      mv "$file" "./services/dotnet-agent-engine/src/$dirname/"; \
    done && \
    for file in ./services/dotnet-agent-engine/tests/*.csproj; do \
      dirname=$(basename "$file" .csproj); \
      mkdir -p "./services/dotnet-agent-engine/tests/$dirname"; \
      mv "$file" "./services/dotnet-agent-engine/tests/$dirname/"; \
    done

WORKDIR /app/services/dotnet-agent-engine
RUN dotnet restore

# Copy entire source and publish
WORKDIR /app
COPY services/dotnet-agent-engine ./services/dotnet-agent-engine
WORKDIR /app/services/dotnet-agent-engine
RUN dotnet publish src/Architect.GrpcService/Architect.GrpcService.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Minimal Distroless/Chiseled Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

USER app
COPY --from=build /app/publish .

EXPOSE 5000 5001
ENV ASPNETCORE_URLS="http://+:5000;http://+:5001"
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Architect.GrpcService.dll"]
