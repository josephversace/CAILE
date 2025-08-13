
#!/usr/bin/env bash
set -e
sudo apt update
sudo apt install -y python3-pip docker.io
pip install fastapi uvicorn sentence-transformers qdrant-client
sudo systemctl enable --now docker || true
echo "WSL setup complete. Next: bash scripts/start_qdrant.sh && python3 scripts/embed_service.py"
