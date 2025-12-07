# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copiar csproj e restaurar
COPY ["LibrarySystem.Api/LibrarySystem.Api.csproj", "LibrarySystem.Api/"]
COPY ["LibrarySystem.Contracts/LibrarySystem.Contracts.csproj", "LibrarySystem.Contracts/"]

RUN dotnet restore "LibrarySystem.Api/LibrarySystem.Api.csproj"

# 2. Copiar código fonte
COPY . .

# 3. Publish
WORKDIR "/src/LibrarySystem.Api"
RUN dotnet publish "LibrarySystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio Final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Segurança: Usuário não-root
USER $APP_UID

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LibrarySystem.Api.dll"]