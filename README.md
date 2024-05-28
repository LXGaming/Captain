# Captain*

[![License](https://img.shields.io/github/license/LXGaming/Captain?label=License&cacheSeconds=86400)](https://github.com/LXGaming/Captain/blob/main/LICENSE)
[![Docker Hub](https://img.shields.io/docker/v/lxgaming/captain/latest?label=Docker%20Hub)](https://hub.docker.com/r/lxgaming/captain)

## Usage
### docker-compose
Download and use [config.json](https://raw.githubusercontent.com/LXGaming/Captain/main/LXGaming.Captain/config.json)
```yaml
services:
  captain:
    container_name: captain
    image: lxgaming/captain:latest
    restart: unless-stopped
    volumes:
      - /path/to/captain/logs:/app/logs
      - /path/to/captain/config.json:/app/config.json
      - /var/run/docker.sock:/var/run/docker.sock
```

## License
Captain is licensed under the [Apache 2.0](https://github.com/LXGaming/Captain/blob/main/LICENSE) license.

###### *Assistant to the Captain