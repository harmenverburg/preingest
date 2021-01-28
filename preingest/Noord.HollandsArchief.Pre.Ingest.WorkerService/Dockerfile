#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-focal AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["Noord.HollandsArchief.Pre.Ingest.WorkerService/Noord.HollandsArchief.Pre.Ingest.WorkerService.csproj", "Noord.HollandsArchief.Pre.Ingest.WorkerService/"]
RUN dotnet restore "Noord.HollandsArchief.Pre.Ingest.WorkerService/Noord.HollandsArchief.Pre.Ingest.WorkerService.csproj"
COPY . .
WORKDIR "/src/Noord.HollandsArchief.Pre.Ingest.WorkerService"
RUN dotnet build "Noord.HollandsArchief.Pre.Ingest.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Noord.HollandsArchief.Pre.Ingest.WorkerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Noord.HollandsArchief.Pre.Ingest.WorkerService.dll"]