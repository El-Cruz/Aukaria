# Etapa 1: Base de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Etapa 2: Compilación y restauración
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["Aukaria.Api/Aukaria.Api.csproj", "Aukaria.Api/"]
# Si tienes proyectos de capas separadas, descomenta estas líneas:
# COPY ["Aukaria.Domain/Aukaria.Domain.csproj", "Aukaria.Domain/"]
# COPY ["Aukaria.Application/Aukaria.Application.csproj", "Aukaria.Application/"]
# COPY ["Aukaria.Infrastructure/Aukaria.Infrastructure.csproj", "Aukaria.Infrastructure/"]

RUN dotnet restore "Aukaria.Api/Aukaria.Api.csproj"

# Copiar todo el código fuente y compilar
COPY . .
WORKDIR "/src/Aukaria.Api"
RUN dotnet build "Aukaria.Api.csproj" -c Release -o /app/build

# Etapa 3: Publicación de binarios
FROM build AS publish
RUN dotnet publish "Aukaria.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 4: Imagen final ligera
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aukaria.Api.dll"]