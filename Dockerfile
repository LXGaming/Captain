# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH
WORKDIR /src

COPY *.sln ./
COPY LXGaming.Captain/*.csproj LXGaming.Captain/
RUN dotnet restore LXGaming.Captain --arch $TARGETARCH

COPY LXGaming.Captain/ LXGaming.Captain/
RUN dotnet publish LXGaming.Captain --arch $TARGETARCH --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
RUN apk add --no-cache --upgrade tzdata
WORKDIR /app
COPY --from=build /app ./
COPY --from=docker:dind /usr/local/bin/docker /usr/local/bin/
ENTRYPOINT ["./LXGaming.Captain"]