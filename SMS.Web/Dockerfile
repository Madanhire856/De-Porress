# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
# RDS uses its own CA roots. Trust the official bundle while retaining server
# certificate validation (TrustServerCertificate=False) in SQL connections.
ADD https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem /tmp/rds-bundle.pem
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates mawk \
    && mkdir -p /usr/local/share/ca-certificates/rds \
    && awk '/-----BEGIN CERTIFICATE-----/ { n++; file=sprintf("/usr/local/share/ca-certificates/rds/rds-%03d.crt", n) } file {print > file} /-----END CERTIFICATE-----/ {close(file);file=""}' /tmp/rds-bundle.pem \
    && update-ca-certificates \
    && rm /tmp/rds-bundle.pem \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SMS.Web/SMS.Web.csproj", "SMS.Web/"]
COPY ["SMS.Lib/SMS.Lib.csproj", "SMS.Lib/"]
COPY ["SMS.Data/SMS.Data.csproj", "SMS.Data/"]
RUN dotnet restore "./SMS.Web/SMS.Web.csproj"
COPY . .
WORKDIR "/src/SMS.Web"
RUN dotnet build "./SMS.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SMS.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SMS.Web.dll"]
