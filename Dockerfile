# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el .csproj primero para que "dotnet restore" quede cacheado entre builds
# mientras no cambien las dependencias (evita re-descargar NuGet en cada cambio de código).
COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Documental — Railway inyecta la variable PORT real en tiempo de ejecución y Program.cs
# hace que Kestrel escuche ahí. Si esa variable no existe (ej. "docker run" local sin
# pasarla), la imagen base de .NET 8 ya escucha en 8080 por su cuenta.
EXPOSE 8080

ENTRYPOINT ["dotnet", "proj-daw-2026-backend.dll"]
