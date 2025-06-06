FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

USER 1001:1001

WORKDIR /app

ARG dCI
ENV dCI=${CI}

RUN echo "++ CI: $dCI"

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["APKonsult/APKonsult.csproj", "APKonsult/"]
RUN dotnet restore "./APKonsult/APKonsult.csproj"
COPY . .
WORKDIR "/src/APKonsult"
RUN dotnet build "./APKonsult.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./APKonsult.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "APKonsult.dll"]