﻿FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETPLATFORM
WORKDIR /src

COPY *.sln .
COPY LXGaming.Captain/*.csproj ./LXGaming.Captain/
RUN case "$TARGETPLATFORM" in \
        linux/amd64) RUNTIME=linux-musl-x64 ;; \
        linux/arm64) RUNTIME=linux-musl-arm64 ;; \
        *) echo "Unsupported Platform: $TARGETPLATFORM"; exit 1 ;; \
    esac && \
    dotnet restore --runtime $RUNTIME

COPY LXGaming.Captain/. ./LXGaming.Captain/
WORKDIR /src/LXGaming.Captain
RUN case "$TARGETPLATFORM" in \
        linux/amd64) RUNTIME=linux-musl-x64 ;; \
        linux/arm64) RUNTIME=linux-musl-arm64 ;; \
        *) echo "Unsupported Platform: $TARGETPLATFORM"; exit 1 ;; \
    esac && \
    dotnet publish --configuration Release --no-restore --output /app --runtime $RUNTIME --self-contained true

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=build /app ./
COPY --from=docker:dind /usr/local/bin/docker /usr/local/bin/
ENTRYPOINT ["./LXGaming.Captain"]