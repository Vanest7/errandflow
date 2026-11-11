FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY backend/ ./backend/
RUN dotnet restore backend/src/ErrandFlow.Api/ErrandFlow.Api.csproj
RUN dotnet publish backend/src/ErrandFlow.Api/ErrandFlow.Api.csproj \
    --configuration Release --output /output --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /output/ ./
USER $APP_UID
EXPOSE 8443
ENTRYPOINT ["dotnet", "ErrandFlow.Api.dll"]
