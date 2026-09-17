FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Pharmaceutical.Core/Pharmaceutical.Core.csproj Pharmaceutical.Core/
COPY Pharmaceutical.Infrastructure/Pharmaceutical.Infrastructure.csproj Pharmaceutical.Infrastructure/
COPY Pharmaceutical.Services/Pharmaceutical.Services.csproj Pharmaceutical.Services/
COPY Pharmaceutical.WebAPI/Pharmaceutical.WebAPI.csproj Pharmaceutical.WebAPI/

RUN dotnet restore Pharmaceutical.WebAPI/Pharmaceutical.WebAPI.csproj

COPY Pharmaceutical.Core/ Pharmaceutical.Core/
COPY Pharmaceutical.Infrastructure/ Pharmaceutical.Infrastructure/
COPY Pharmaceutical.Services/ Pharmaceutical.Services/
COPY Pharmaceutical.WebAPI/ Pharmaceutical.WebAPI/

RUN dotnet build Pharmaceutical.WebAPI/Pharmaceutical.WebAPI.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish Pharmaceutical.WebAPI/Pharmaceutical.WebAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pharmaceutical.WebAPI.dll"]
