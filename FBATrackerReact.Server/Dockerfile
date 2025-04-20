# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS with-node
RUN apt-get update
RUN apt-get -y install curl
RUN curl -sL https://deb.nodesource.com/setup_22.x | bash
RUN apt-get -y install nodejs

FROM with-node as build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["fbatrackerreact.client/nuget.config", "fbatrackerreact.client/"]
COPY ["FBATrackerReact.Server/FBATrackerReact.Server.csproj", "FBATrackerReact.Server/"]
COPY ["FBATrackerReact.ArgosScraper/FBATrackerReact.ArgosScraper.csproj", "FBATrackerReact.ArgosScraper/"]
COPY ["FBATrackerReact.BootsScraper/FBATrackerReact.BootsScraper.csproj", "FBATrackerReact.BootsScraper/"]
COPY ["FBATrackerReact.BandQScraper/FBATrackerReact.BandQScraper.csproj", "FBATrackerReact.BandQScraper/"]
COPY ["FBATrackerReact.CurrysScraper/FBATrackerReact.CurrysScraper.csproj", "FBATrackerReact.CurrysScraper/"]
COPY ["FBATrackerReact.OverclockersScraper/FBATrackerReact.OverclockersScraper.csproj", "FBATrackerReact.OverclockersScraper/"]
COPY ["FBATrackerReact.SaversScraper/FBATrackerReact.SaversScraper.csproj", "FBATrackerReact.SaversScraper/"]
COPY ["FBATrackerReact.BargainFoxScraper/FBATrackerReact.BargainFoxScraper.csproj", "FBATrackerReact.BargainFoxScraper/"]
COPY ["FBATrackerReact.SharedScraper/FBATrackerReact.SharedScraper.csproj", "FBATrackerReact.SharedScraper/"]
COPY ["FBATrackerReact.Models/FBATrackerReact.Models.csproj", "FBATrackerReact.Models/"]
COPY ["fbatrackerreact.client/fbatrackerreact.client.esproj", "fbatrackerreact.client/"]
RUN dotnet restore "./FBATrackerReact.Server/FBATrackerReact.Server.csproj"
COPY . .
WORKDIR "/src/FBATrackerReact.Server"
RUN dotnet build "./FBATrackerReact.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG ASPNETCORE_ENVIRONMENT=Development
RUN dotnet publish "./FBATrackerReact.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FBATrackerReact.Server.dll"]