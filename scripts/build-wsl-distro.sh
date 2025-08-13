#!/bin/bash
# WSL2 Distribution Builder for IIM
# Creates a pre-configured Ubuntu 22.04 image with all services installed

set -e

echo "=== IIM WSL2 Distribution Builder ==="
echo "This script creates a custom WSL2 distribution with all IIM services pre-installed"
echo ""

# Configuration
DISTRO_NAME="IIM-Ubuntu-Build"
EXPORT_NAME="IIM-Ubuntu-22.04"
WORK_DIR="/tmp/iim-wsl-build"
OUTPUT_DIR="./dist"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Step 1: Download Ubuntu base image
download_base_image() {
    log_info "Downloading Ubuntu 22.04 base image..."
    
    mkdir -p $WORK_DIR
    cd $WORK_DIR
    
    if [ ! -f "ubuntu-22.04-wsl.tar.gz" ]; then
        wget -O ubuntu-22.04-wsl.tar.gz https://cloud-images.ubuntu.com/wsl/jammy/current/ubuntu-jammy-wsl-amd64-wsl.rootfs.tar.gz
    else
        log_info "Base image already downloaded"
    fi
}

# Step 2: Import base image to WSL
import_base_image() {
    log_info "Importing base image to WSL..."
    
    # Check if distro already exists
    if wsl --list --quiet | grep -q "^${DISTRO_NAME}$"; then
        log_warn "Distro $DISTRO_NAME already exists, removing..."
        wsl --unregister $DISTRO_NAME
    fi
    
    # Import the distro
    wsl --import $DISTRO_NAME $WORK_DIR/distro ubuntu-22.04-wsl.tar.gz --version 2
    
    log_info "Base image imported successfully"
}

# Step 3: Configure the distro
configure_distro() {
    log_info "Configuring distro..."
    
    # Create configuration script
    cat > $WORK_DIR/configure.sh << 'CONFIGURE_SCRIPT'
#!/bin/bash
set -e

echo "=== Configuring IIM WSL2 Distribution ==="

# Update system
apt-get update
DEBIAN_FRONTEND=noninteractive apt-get upgrade -y

# Install essential packages
DEBIAN_FRONTEND=noninteractive apt-get install -y \
    curl wget git nano vim htop net-tools iputils-ping \
    build-essential cmake pkg-config \
    python3 python3-pip python3-venv python3-dev \
    docker.io docker-compose \
    nginx supervisor \
    postgresql postgresql-contrib \
    redis-server \
    ffmpeg imagemagick \
    tesseract-ocr tesseract-ocr-eng \
    poppler-utils \
    unzip p7zip-full

# Install Python packages
pip3 install --upgrade pip setuptools wheel
pip3 install \
    fastapi uvicorn[standard] \
    sentence-transformers transformers \
    qdrant-client chromadb \
    langchain langchain-community \
    pandas numpy scipy scikit-learn \
    pillow opencv-python-headless \
    pytesseract PyPDF2 python-docx \
    redis celery \
    httpx aiofiles \
    python-multipart python-jose[cryptography] \
    sqlalchemy alembic \
    pytest pytest-asyncio

# Install Node.js (for potential web UI needs)
curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
apt-get install -y nodejs

# Configure Docker
usermod -aG docker root 2>/dev/null || true
systemctl enable docker || true

# Pull essential Docker images
systemctl start docker || service docker start
docker pull qdrant/qdrant:v1.12.4
docker pull redis:7-alpine
docker pull postgres:15-alpine
docker pull ollama/ollama:latest

# Create IIM directories
mkdir -p /opt/iim/{models,data,logs,scripts,services}
mkdir -p /var/log/iim
mkdir -p /etc/iim

# Create IIM user
useradd -m -s /bin/bash -G sudo,docker iim || true
echo 'iim:iim' | chpasswd
echo 'iim ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# Install embedding service
cat > /opt/iim/services/embed_service.py << 'EMBED_SERVICE'
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import logging
import os
import json
from typing import List, Optional

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title='IIM Embedding Service', version='1.0.0')

app.add_middleware(
    CORSMiddleware,
    allow_origins=['*'],
    allow_methods=['*'],
    allow_headers=['*'],
)

# Model management
models = {}
default_model = os.getenv('DEFAULT_EMBED_MODEL', 'all-MiniLM-L6-v2')

def load_model(model_name: str):
    if model_name not in models:
        logger.info(f'Loading model: {model_name}')
        models[model_name] = SentenceTransformer(model_name)
    return models[model_name]

# Pre-load default model
load_model(default_model)

class EmbedRequest(BaseModel):
    text: str
    model: Optional[str] = None
    
class BatchEmbedRequest(BaseModel):
    texts: List[str]
    model: Optional[str] = None

@app.get('/health')
def health():
    return {
        'status': 'healthy',
        'loaded_models': list(models.keys()),
        'default_model': default_model
    }

@app.post('/embed')
def embed(request: EmbedRequest):
    try:
        model_name = request.model or default_model
        model = load_model(model_name)
        embedding = model.encode(request.text).tolist()
        return {'embedding': embedding, 'model': model_name, 'dimension': len(embedding)}
    except Exception as e:
        logger.error(f'Embedding failed: {e}')
        raise HTTPException(status_code=500, detail=str(e))

@app.post('/embed/batch')
def embed_batch(request: BatchEmbedRequest):
    try:
        model_name = request.model or default_model
        model = load_model(model_name)
        embeddings = model.encode(request.texts).tolist()
        return {
            'embeddings': embeddings,
            'model': model_name,
            'dimension': len(embeddings[0]) if embeddings else 0,
            'count': len(embeddings)
        }
    except Exception as e:
        logger.error(f'Batch embedding failed: {e}')
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == '__main__':
    import uvicorn
    uvicorn.run(app, host='0.0.0.0', port=8081)
EMBED_SERVICE

chmod +x /opt/iim/services/embed_service.py

# Create supervisor configuration
cat > /etc/supervisor/conf.d/iim.conf << 'SUPERVISOR_CONF'
[program:embed_service]
command=python3 /opt/iim/services/embed_service.py
directory=/opt/iim/services
user=iim
autostart=true
autorestart=true
stderr_logfile=/var/log/iim/embed_service.err.log
stdout_logfile=/var/log/iim/embed_service.out.log

[program:qdrant]
command=docker run --rm --name qdrant -p 6333:6333 -v /opt/iim/data/qdrant:/qdrant/storage qdrant/qdrant:v1.12.4
user=root
autostart=true
autorestart=true
stderr_logfile=/var/log/iim/qdrant.err.log
stdout_logfile=/var/log/iim/qdrant.out.log
SUPERVISOR_CONF

# Create startup script
cat > /opt/iim/scripts/startup.sh << 'STARTUP_SCRIPT'
#!/bin/bash
echo "Starting IIM services..."

# Start Docker
service docker start

# Wait for Docker
sleep 5

# Start Redis
service redis-server start

# Start PostgreSQL
service postgresql start

# Start supervisor (manages Python services)
service supervisor start

# Start Qdrant if not running
if ! docker ps | grep -q qdrant; then
    docker run -d --name qdrant \
        -p 6333:6333 \
        -v /opt/iim/data/qdrant:/qdrant/storage \
        --restart always \
        qdrant/qdrant:v1.12.4
fi

echo "IIM services started successfully"
STARTUP_SCRIPT

chmod +x /opt/iim/scripts/startup.sh

# Create WSL configuration
cat > /etc/wsl.conf << 'WSL_CONF'
[boot]
systemd=true
command="/opt/iim/scripts/startup.sh"

[network]
generateResolvConf=true
generateHosts=true

[interop]
enabled=true
appendWindowsPath=true

[user]
default=iim
WSL_CONF

# Download sample models (lightweight ones)
log_info "Downloading sample models..."
python3 -c "
from sentence_transformers import SentenceTransformer
print('Downloading all-MiniLM-L6-v2...')
model = SentenceTransformer('all-MiniLM-L6-v2')
model.save('/opt/iim/models/all-MiniLM-L6-v2')
print('Model downloaded successfully')
"

# Clean up
apt-get clean
rm -rf /var/lib/apt/lists/*
rm -rf /tmp/*

echo "=== Configuration complete ==="
CONFIGURE_SCRIPT

    # Run configuration in the distro
    wsl -d $DISTRO_NAME -u root bash < $WORK_DIR/configure.sh
}

# Step 4: Test the distro
test_distro() {
    log_info "Testing distro configuration..."
    
    # Test Docker
    if wsl -d $DISTRO_NAME -u root docker --version > /dev/null 2>&1; then
        log_info "✓ Docker is working"
    else
        log_error "✗ Docker test failed"
    fi
    
    # Test Python
    if wsl -d $DISTRO_NAME -u root python3 --version > /dev/null 2>&1; then
        log_info "✓ Python is working"
    else
        log_error "✗ Python test failed"
    fi
    
    # Test embedding service
    wsl -d $DISTRO_NAME -u root bash -c "cd /opt/iim/services && python3 embed_service.py &"
    sleep 5
    
    if curl -s http://localhost:8081/health > /dev/null 2>&1; then
        log_info "✓ Embedding service is working"
    else
        log_warn "✗ Embedding service test failed (may need manual start)"
    fi
    
    # Kill test service
    wsl -d $DISTRO_NAME -u root pkill -f embed_service || true
}

# Step 5: Export the configured distro
export_distro() {
    log_info "Exporting configured distro..."
    
    mkdir -p $OUTPUT_DIR
    
    # Stop all services in the distro
    wsl -d $DISTRO_NAME -u root bash -c "service docker stop || true; service supervisor stop || true"
    
    # Export the distro
    wsl --export $DISTRO_NAME $OUTPUT_DIR/${EXPORT_NAME}.tar.gz
    
    # Create metadata file
    cat > $OUTPUT_DIR/${EXPORT_NAME}.json << EOF
{
    "name": "${EXPORT_NAME}",
    "version": "1.0.0",
    "build_date": "$(date -Iseconds)",
    "base_image": "Ubuntu 22.04",
    "size_compressed": $(stat -f%z $OUTPUT_DIR/${EXPORT_NAME}.tar.gz 2>/dev/null || stat -c%s $OUTPUT_DIR/${EXPORT_NAME}.tar.gz),
    "included_services": [
        "qdrant",
        "embedding_service",
        "docker",
        "redis",
        "postgresql"
    ],
    "python_packages": [
        "fastapi",
        "sentence-transformers",
        "qdrant-client",
        "langchain"
    ],
    "docker_images": [
        "qdrant/qdrant:v1.12.4",
        "ollama/ollama:latest",
        "redis:7-alpine",
        "postgres:15-alpine"
    ]
}
EOF
    
    log_info "Distro exported to $OUTPUT_DIR/${EXPORT_NAME}.tar.gz"
    log_info "Size: $(du -h $OUTPUT_DIR/${EXPORT_NAME}.tar.gz | cut -f1)"
}

# Step 6: Create installer script
create_installer() {
    log_info "Creating installer script..."
    
    cat > $OUTPUT_DIR/install-iim-wsl.ps1 << 'INSTALLER'
# IIM WSL2 Distribution Installer
param(
    [string]$DistroName = "IIM-Ubuntu",
    [string]$InstallPath = "$env:LOCALAPPDATA\IIM\WSL"
)

Write-Host "=== IIM WSL2 Distribution Installer ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator" -ForegroundColor Yellow
    exit 1
}

# Check WSL2 installation
Write-Host "Checking WSL2 installation..." -ForegroundColor Yellow
$wslVersion = wsl --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "WSL2 is not installed. Installing..." -ForegroundColor Yellow
    wsl --install
    Write-Host "Please restart your computer and run this script again" -ForegroundColor Yellow
    exit 0
}

# Check if distro already exists
$existingDistros = wsl --list --quiet
if ($existingDistros -contains $DistroName) {
    Write-Host "Distro $DistroName already exists" -ForegroundColor Yellow
    $response = Read-Host "Do you want to remove it and reinstall? (y/n)"
    if ($response -eq 'y') {
        Write-Host "Unregistering existing distro..." -ForegroundColor Yellow
        wsl --unregister $DistroName
    } else {
        exit 0
    }
}

# Create installation directory
Write-Host "Creating installation directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null

# Import the distro
$tarPath = Join-Path $PSScriptRoot "IIM-Ubuntu-22.04.tar.gz"
if (-not (Test-Path $tarPath)) {
    Write-Host "Distribution file not found: $tarPath" -ForegroundColor Red
    exit 1
}

Write-Host "Importing IIM distribution (this may take several minutes)..." -ForegroundColor Yellow
wsl --import $DistroName $InstallPath $tarPath --version 2

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Distribution imported successfully" -ForegroundColor Green
    
    # Set as default if no other distro exists
    $distroCount = (wsl --list --quiet | Measure-Object).Count
    if ($distroCount -eq 1) {
        wsl --set-default $DistroName
        Write-Host "✓ Set as default distribution" -ForegroundColor Green
    }
    
    # Start services
    Write-Host "Starting IIM services..." -ForegroundColor Yellow
    wsl -d $DistroName -u root /opt/iim/scripts/startup.sh
    
    # Get IP address
    $wslIp = wsl -d $DistroName hostname -I | ForEach-Object { $_.Trim() }
    
    Write-Host ""
    Write-Host "=== Installation Complete ===" -ForegroundColor Green
    Write-Host "Distribution Name: $DistroName" -ForegroundColor Cyan
    Write-Host "WSL IP Address: $wslIp" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Service Endpoints:" -ForegroundColor Yellow
    Write-Host "  Qdrant:    http://${wslIp}:6333" -ForegroundColor White
    Write-Host "  Embedding: http://${wslIp}:8081" -ForegroundColor White
    Write-Host ""
    Write-Host "To enter the distribution:" -ForegroundColor Yellow
    Write-Host "  wsl -d $DistroName" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "✗ Failed to import distribution" -ForegroundColor Red
    exit 1
}
INSTALLER
    
    log_info "Installer script created"
}

# Step 7: Clean up
cleanup() {
    log_info "Cleaning up..."
    
    # Unregister the build distro
    wsl --unregister $DISTRO_NAME
    
    # Remove work directory
    rm -rf $WORK_DIR
    
    log_info "Cleanup complete"
}

# Main execution
main() {
    echo "Starting build process..."
    echo ""
    
    download_base_image
    import_base_image
    configure_distro
    test_distro
    export_distro
    create_installer
    cleanup
    
    echo ""
    echo "=== Build Complete ===" 
    echo "Output files:"
    echo "  - $OUTPUT_DIR/${EXPORT_NAME}.tar.gz (WSL2 distribution)"
    echo "  - $OUTPUT_DIR/${EXPORT_NAME}.json (metadata)"
    echo "  - $OUTPUT_DIR/install-iim-wsl.ps1 (installer script)"
    echo ""
    echo "To install on target machines:"
    echo "  1. Copy the $OUTPUT_DIR folder to the target machine"
    echo "  2. Run PowerShell as Administrator"
    echo "  3. Execute: .\\install-iim-wsl.ps1"
    echo ""
    log_info "Build successful!"
}

# Run main function
main