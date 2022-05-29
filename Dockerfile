FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY *.sln .
COPY LXGaming.Captain/*.csproj ./LXGaming.Captain/
RUN dotnet restore

COPY LXGaming.Captain/. ./LXGaming.Captain/
WORKDIR /src/LXGaming.Captain
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "LXGaming.Captain.dll"]