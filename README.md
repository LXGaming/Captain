# Captain*

[![License](https://lxgaming.github.io/badges/License-Apache%202.0-blue.svg)](https://www.apache.org/licenses/LICENSE-2.0)
[![Docker Pulls](https://img.shields.io/docker/pulls/lxgaming/captain)](https://hub.docker.com/r/lxgaming/captain)

## Usage
### docker-compose
```yaml
version: "3"
services:
  captain:
    container_name: captain
    environment:
      - TZ=Pacific/Auckland
    image: lxgaming/captain:latest
    restart: unless-stopped
    volumes:
      - /path/to/captain/logs:/app/logs
      - /path/to/captain/config.json:/app/config.json
      - /var/run/docker.sock:/var/run/docker.sock
```

## License
Captain is licensed under the [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) license.

###### *Assistant to the Captain