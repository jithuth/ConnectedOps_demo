#!/usr/bin/env bash
# ==============================================================================
# ConnectedOps - Enable Free SSL/TLS (HTTPS) via Let's Encrypt and Certbot
# Usage: sudo bash aws/enable-ssl.sh yourdomain.com admin@yourdomain.com
# ==============================================================================

set -e

DOMAIN=$1
EMAIL=$2

CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

if [ "$EUID" -ne 0 ]; then
  echo -e "${RED}[ERROR] Please run with sudo or as root:${NC}"
  echo "sudo bash aws/enable-ssl.sh <domain> <email>"
  exit 1
fi

if [ -z "$DOMAIN" ]; then
  read -p "Enter your custom domain name (e.g. fleet.mycompany.com): " DOMAIN
fi

if [ -z "$EMAIL" ]; then
  read -p "Enter your admin email for SSL renewal notifications: " EMAIL
fi

echo -e "${CYAN}Installing Nginx and Certbot for domain: $DOMAIN...${NC}"

if [ -f /etc/os-release ]; then
  . /etc/os-release
  OS=$ID
fi

if [[ "$OS" == "amzn" || "$OS" == "fedora" || "$OS" == "rhel" ]]; then
  dnf install -y nginx certbot python3-certbot-nginx || yum install -y nginx certbot python3-certbot-nginx
elif [[ "$OS" == "ubuntu" || "$OS" == "debian" ]]; then
  apt-get update -y
  apt-get install -y nginx certbot python3-certbot-nginx
fi

# Copy Nginx base configuration
cp aws/nginx.conf /etc/nginx/conf.d/connectedops.conf || cp aws/nginx.conf /etc/nginx/sites-available/connectedops.conf

# Ensure nginx starts
systemctl enable --now nginx

# Obtain SSL Certificate
echo -e "${CYAN}Requesting Let's Encrypt Certificate for $DOMAIN...${NC}"
certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos --email "$EMAIL" --redirect

echo -e "${GREEN}=========================================================================${NC}"
echo -e "${GREEN}SSL SUCCESS! Your ConnectedOps Platform is now secured with HTTPS:${NC}"
echo -e "${CYAN}https://${DOMAIN}${NC}"
echo -e "${GREEN}=========================================================================${NC}"
