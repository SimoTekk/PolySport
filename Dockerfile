# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Erst nur die Projektdateien kopieren, damit der Restore-Layer
# im Cache bleibt, solange sich die Abhängigkeiten nicht ändern.
COPY PolySport.sln .
COPY PolySport/PolySport.csproj PolySport/
RUN dotnet restore

COPY . .
RUN dotnet publish PolySport/PolySport.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Laufzeit ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Das Basis-Image läuft bereits als Benutzer "app" (nicht root)
# und hört auf Port 8080.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "PolySport.dll"]
