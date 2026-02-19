FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SistemaVotoAPI/SistemaVotoAPI.csproj SistemaVotoAPI/
COPY SistemaVotoModelos/SistemaVotoModelos.csproj SistemaVotoModelos/
RUN dotnet restore SistemaVotoAPI/SistemaVotoAPI.csproj

COPY . .
RUN dotnet publish SistemaVotoAPI/SistemaVotoAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SistemaVotoAPI.dll"]
