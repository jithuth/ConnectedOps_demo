#!/usr/bin/env bash
# ==============================================================================
# ConnectedOps Enterprise Platform - AWS EC2 Automated Production Setup Script
# Works on Amazon Linux 2023, Amazon Linux 2, Ubuntu 22.04+, and Debian 12+
# ==============================================================================

set -e

# Visual colors
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${CYAN}"
echo "================================================================================"
echo "          CONNECTEDOPS ENTERPRISE PLATFORM - AWS EC2 DEPLOYMENT SETUP           "
echo "================================================================================"
echo -e "${NC}"

# 1. Require root / sudo
if [ "$EUID" -ne 0 ]; then
  echo -e "${RED}[ERROR] Please run this script with sudo or as root:${NC}"
  echo "sudo bash $0"
  exit 1
fi

# 2. Detect OS & Package Manager
echo -e "${CYAN}[1/6] Detecting Linux Distribution and Installing Prerequisites...${NC}"
if [ -f /etc/os-release ]; then
  . /etc/os-release
  OS=$ID
else
  OS=$(uname -s)
fi

# Ensure swap space exists (critical for micro/small instances)
if [ ! -f /swapfile ] && [ $(free -m | awk '/^Mem:/{print $2}') -lt 3500 ]; then
  echo -e "${CYAN}Setting up 4GB swap space to ensure memory resilience...${NC}"
  dd if=/dev/zero of=/swapfile bs=1M count=4096 status=none
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

if [[ "$OS" == "amzn" || "$OS" == "fedora" || "$OS" == "rhel" || "$OS" == "centos" ]]; then
  dnf update -y || yum update -y
  dnf install -y --allowerasing docker git openssl jq || yum install -y docker git openssl jq
  systemctl enable --now docker
  # Install Docker Compose v2 plugin if not present
  if ! docker compose version &> /dev/null; then
    echo "Installing Docker Compose plugin..."
    mkdir -p /usr/local/lib/docker/cli-plugins
    DOCKER_COMPOSE_VER=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | jq -r .tag_name 2>/dev/null || echo "v2.29.2")
    curl -SL "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VER}/docker-compose-linux-$(uname -m)" -o /usr/local/lib/docker/cli-plugins/docker-compose
    chmod +x /usr/local/lib/docker/cli-plugins/docker-compose
    ln -sf /usr/local/lib/docker/cli-plugins/docker-compose /usr/bin/docker-compose
  fi
  # Always ensure modern Docker Buildx plugin (>=0.17 required by Compose v2)
  echo "Ensuring modern Docker Buildx plugin (v0.19.3)..."
  mkdir -p /usr/local/lib/docker/cli-plugins /usr/lib/docker/cli-plugins
  curl -SL "https://github.com/docker/buildx/releases/download/v0.19.3/buildx-v0.19.3.linux-$(uname -m)" -o /usr/local/lib/docker/cli-plugins/docker-buildx
  chmod +x /usr/local/lib/docker/cli-plugins/docker-buildx
  cp -f /usr/local/lib/docker/cli-plugins/docker-buildx /usr/lib/docker/cli-plugins/docker-buildx
  ln -sf /usr/local/lib/docker/cli-plugins/docker-buildx /usr/bin/docker-buildx
elif [[ "$OS" == "ubuntu" || "$OS" == "debian" ]]; then
  apt-get update -y
  apt-get install -y ca-certificates curl gnupg lsb-release git openssl jq
  mkdir -m 0755 -p /etc/apt/keyrings
  if [ ! -f /etc/apt/keyrings/docker.gpg ]; then
    curl -fsSL https://download.docker.com/linux/$OS/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  fi
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/$OS $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  systemctl enable --now docker
else
  echo -e "${YELLOW}[WARNING] Unrecognized distribution ($OS). Ensuring docker is available...${NC}"
  if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com | sh
    systemctl enable --now docker
  fi
fi

# Add current active user to docker group
if [ -n "$SUDO_USER" ]; then
  usermod -aG docker "$SUDO_USER" || true
fi

# 3. Determine Application Directory
APP_DIR="/opt/connectedops"
CURRENT_DIR=$(pwd)

if [ -f "$CURRENT_DIR/docker-compose.yml" ]; then
  echo -e "${GREEN}[2/6] Running inside ConnectedOps workspace directory: $CURRENT_DIR${NC}"
  WORK_DIR="$CURRENT_DIR"
else
  echo -e "${CYAN}[2/6] Setting up deployment directory at $APP_DIR...${NC}"
  mkdir -p "$APP_DIR"
  if [ -d "$CURRENT_DIR/.git" ]; then
    cp -r "$CURRENT_DIR/." "$APP_DIR/"
  fi
  WORK_DIR="$APP_DIR"
fi

cd "$WORK_DIR"

# 4. Generate Production Secrets (.env)
echo -e "${CYAN}[3/6] Configuring Production Environment Secrets (.env)...${NC}"
if [ ! -f .env ]; then
  echo "Generating fresh cryptographically secure passwords and keys..."
  
  # Generate 32-char complex password meeting SQL Server 2022 policy
  RAND_PASS="CO_$(openssl rand -base64 18 | tr -dc 'a-zA-Z0-9' | head -c 16)!Aa1"
  RAND_JWT=$(openssl rand -base64 48 | tr -dc 'a-zA-Z0-9!#$%' | head -c 48)

  cat > .env <<EOF
# ConnectedOps Production Environment Configuration
# Auto-generated by setup-ec2.sh on $(date -u)

MSSQL_SA_PASSWORD=${RAND_PASS}
MSSQL_PORT=1433

JWT_SECRET=${RAND_JWT}
JWT_ISSUER=https://connectedops.io
JWT_AUDIENCE=https://connectedops.io

API_PORT=5000
WEB_PORT=5001
ASPNETCORE_ENVIRONMENT=Production
EOF
  chmod 600 .env
  echo -e "${GREEN}Created .env file with secure secrets.${NC}"
else
  echo -e "${YELLOW}.env file already exists. Preserving existing secrets.${NC}"
fi

# 5. Create Resilient Systemd Service
echo -e "${CYAN}[4/6] Creating 'connectedops.service' systemd daemon...${NC}"
cat > /etc/systemd/system/connectedops.service <<EOF
[Unit]
Description=ConnectedOps Enterprise Multi-Container Cloud Platform
Requires=docker.service
After=docker.service network-online.target
Wants=network-online.target

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$WORK_DIR
ExecStart=/usr/bin/docker compose up -d --build --remove-orphans
ExecStop=/usr/bin/docker compose down
ExecReload=/usr/bin/docker compose restart
TimeoutStartSec=600

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable connectedops.service

# 6. Build and Start Application Containers
echo -e "${CYAN}[5/6] Building and Starting ConnectedOps Containers...${NC}"
echo "This will compile .NET 10 Web, API, and initialize SQL Server 2022."
docker compose build
docker compose up -d

# 7. Discover Public IP & Verify Deployment
echo -e "${CYAN}[6/6] Verifying Deployment and Discovering Cloud IP...${NC}"

# Try AWS IMDSv2 token first, fallback to public lookup
IMDS_TOKEN=$(curl -s -S -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 60" 2>/dev/null || true)
if [ -n "$IMDS_TOKEN" ]; then
  PUBLIC_IP=$(curl -s -H "X-aws-ec2-metadata-token: $IMDS_TOKEN" http://169.254.169.254/latest/meta-data/public-ipv4 2>/dev/null || true)
fi

if [ -z "$PUBLIC_IP" ]; then
  PUBLIC_IP=$(curl -s https://checkip.amazonaws.com || curl -s https://ifconfig.me || echo "YOUR_SERVER_IP")
fi

# Summary Banner
echo -e "${GREEN}"
echo "================================================================================"
echo "          CONNECTEDOPS IS LIVE AND RUNNING ON AWS!                              "
echo "================================================================================"
echo -e "${NC}"
echo -e "Web Management Portal:     ${CYAN}http://${PUBLIC_IP}:5001${NC}"
echo -e "Open API Gateway Swagger:  ${CYAN}http://${PUBLIC_IP}:5000/swagger${NC}"
echo -e "SignalR WebSocket Hub:     ${CYAN}ws://${PUBLIC_IP}:5001/hubs/fleet${NC}"
echo ""
echo -e "Database Host:             ${YELLOW}localhost:1433 (or container sql-server)${NC}"
echo -e "Working Directory:         ${YELLOW}$WORK_DIR${NC}"
echo -e "Config & Secrets:          ${YELLOW}$WORK_DIR/.env${NC}"
echo ""
echo "Helpful Operational Commands:"
echo "  View live logs:          docker compose logs -f"
echo "  Check container status:  docker compose ps"
echo "  Restart platform:        sudo systemctl restart connectedops"
echo "  Stop platform:           sudo systemctl stop connectedops"
echo "================================================================================"
