FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY LogCentral/ ./LogCentral/
WORKDIR /src/LogCentral
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV LOG_DATA_DIR=/app/logs
EXPOSE 8080
VOLUME ["/app/logs"]
ENTRYPOINT ["dotnet", "LogCentral.dll"]
