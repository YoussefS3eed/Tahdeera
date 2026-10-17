FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for layer caching
COPY ["Tahdeera.slnx", "./"]
COPY ["src/Tahdeera.Domain/Tahdeera.Domain.csproj", "src/Tahdeera.Domain/"]
COPY ["src/Tahdeera.Application/Tahdeera.Application.csproj", "src/Tahdeera.Application/"]
COPY ["src/Tahdeera.Infrastructure/Tahdeera.Infrastructure.csproj", "src/Tahdeera.Infrastructure/"]
COPY ["src/Tahdeera.API/Tahdeera.API.csproj", "src/Tahdeera.API/"]

# Restore dependencies
RUN dotnet restore "src/Tahdeera.API/Tahdeera.API.csproj"

# Copy full source and build
COPY . .
WORKDIR "/src/src/Tahdeera.API"
RUN dotnet build "Tahdeera.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Tahdeera.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tahdeera.API.dll"]
