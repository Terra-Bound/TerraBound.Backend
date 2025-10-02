#############
# Base Image
#############
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

USER $APP_UID
WORKDIR /app

EXPOSE 8080
EXPOSE 8081
EXPOSE 9050

#############
# Build Svelte
#############
FROM node:20 AS build-frontend

WORKDIR /src
COPY ["AspNet.Backend/Frontend/package.json", "AspNet.Backend/Frontend/package-lock.json*", "./Frontend/"]

WORKDIR /src/Frontend
RUN npm ci

COPY ["AspNet.Backend/Frontend/", "./"]
RUN npm run build

################
# Build ASP.Net
################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-backend
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["AspNet.Backend/AspNet.Backend.csproj", "./"]
RUN dotnet restore
COPY ["AspNet.Backend/.", "./"]

# Build backend (frontend will be copied after publish)
COPY --from=build-frontend /src/Frontend/dist ./wwwroot/
RUN dotnet build "AspNet.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build  # Build! 

################
# Publish
################
FROM build-backend AS publish

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AspNet.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Copy frontend build into the published wwwroot so it's available at runtime
COPY --from=build-frontend /src/Frontend/dist /app/publish/wwwroot/

################
# Finalize
################
FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build-frontend /src/Frontend/dist /app/wwwroot/
ENTRYPOINT ["dotnet", "AspNet.Backend.dll"]