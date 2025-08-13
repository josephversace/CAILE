
#!/usr/bin/env bash
set -e
docker run --rm -p 6333:6333 -v $HOME/qdrant:/qdrant/storage qdrant/qdrant:v1.12.4
