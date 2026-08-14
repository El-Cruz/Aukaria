FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Aukaria.Domain/Aukaria.Domain.csproj", "Aukaria.Domain/"]
COPY ["Aukaria.Application/Aukaria.Application.csproj", "Aukaria.Application/"]
COPY ["Aukaria.Infrastructure/Aukaria.Infrastructure.csproj", "Aukaria.Infrastructure/"]
COPY ["Aukaria.Api/Aukaria.Api.csproj", "Aukaria.Api/"]
RUN dotnet restore "Aukaria.Api/Aukaria.Api.csproj"
COPY . .
WORKDIR "/src/Aukaria.Api"
RUN dotnet publish "Aukaria.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Aukaria.Api.dll"]