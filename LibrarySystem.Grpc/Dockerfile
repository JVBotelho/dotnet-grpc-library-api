FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["LibrarySystem.Grpc/LibrarySystem.Grpc.csproj", "LibrarySystem.Grpc/"]
COPY ["LibrarySystem.Application/LibrarySystem.Application.csproj", "LibrarySystem.Application/"]
COPY ["LibrarySystem.Domain/LibrarySystem.Domain.csproj", "LibrarySystem.Domain/"]
COPY ["LibrarySystem.Persistence/LibrarySystem.Persistence.csproj", "LibrarySystem.Persistence/"]
COPY ["LibrarySystem.Contracts/LibrarySystem.Contracts.csproj", "LibrarySystem.Contracts/"]

RUN dotnet restore "LibrarySystem.Grpc/LibrarySystem.Grpc.csproj"

COPY . .

WORKDIR "/src/LibrarySystem.Grpc"
RUN dotnet publish "LibrarySystem.Grpc.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LibrarySystem.Grpc.dll"]