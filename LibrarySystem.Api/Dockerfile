# Estágio 1: Build
# Usamos a imagem do SDK do .NET 8 para compilar a aplicação.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Otimização de Cache: Copiar ficheiros de projeto e restaurar dependências primeiro.
# Isto só será executado novamente se os ficheiros .csproj ou .sln mudarem.
COPY ["LibrarySystem.sln", "."]
COPY ["LibrarySystem.Api/LibrarySystem.Api.csproj", "LibrarySystem.Api/"]
COPY ["LibrarySystem.Grpc/LibrarySystem.Grpc.csproj", "LibrarySystem.Grpc/"]
COPY ["LibrarySystem.Persistence/LibrarySystem.Persistence.csproj", "LibrarySystem.Persistence/"]
# CORREÇÃO: Copiar os ficheiros de projeto dos testes e outros projetos
COPY ["LibrarySystem.UnitTests/LibrarySystem.UnitTests.csproj", "LibrarySystem.UnitTests/"]
COPY ["LibrarySystem.IntegrationTests/LibrarySystem.IntegrationTests.csproj", "LibrarySystem.IntegrationTests/"]
COPY ["WarmupTasks/WarmupTasks.csproj", "WarmupTasks/"]
RUN dotnet restore "LibrarySystem.sln"

# Copiar o resto do código fonte.
COPY . .

# Publicar a aplicação para a pasta de saída.
WORKDIR "/src/LibrarySystem.Api"
RUN dotnet publish "LibrarySystem.Api.csproj" -c Release -o /app/publish

# Estágio 2: Final
# Usamos a imagem mais leve do ASP.NET Runtime, que é otimizada para produção.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copiar a aplicação publicada do estágio de build.
COPY --from=build /app/publish .

# Definir o ponto de entrada para executar a aplicação.
ENTRYPOINT ["dotnet", "LibrarySystem.Api.dll"]