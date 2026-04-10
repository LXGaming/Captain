# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH
WORKDIR /src

COPY --link *.sln ./
COPY --link LXGaming.Captain/*.csproj LXGaming.Captain/
RUN dotnet restore LXGaming.Captain --arch $TARGETARCH

COPY --link LXGaming.Captain/ LXGaming.Captain/
RUN dotnet publish LXGaming.Captain --arch $TARGETARCH --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
RUN apk add --no-cache --upgrade tzdata
WORKDIR /app
COPY --from=build --link /app ./
COPY --from=docker:dind --link  /usr/local/bin/docker /usr/local/bin/
ENTRYPOINT ["./LXGaming.Captain"]