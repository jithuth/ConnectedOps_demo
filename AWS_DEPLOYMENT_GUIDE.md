# ConnectedOps Enterprise - Complete AWS Deployment Guide

> **Welcome!** If you have never used Amazon Web Services (AWS) before, **do not worry**. This guide is written specifically for you. Follow these steps sequentially, and you will have your full ConnectedOps platform (Web Portal, REST API Gateway, SignalR Live Streaming, and SQL Server Database) running live in the cloud in under 15 minutes.

---

## Table of Contents
1. [How ConnectedOps Runs on AWS](#1-how-connectedops-runs-on-aws)
2. [What You Need Before Starting](#2-what-you-need-before-starting)
3. [Method 1: The 1-Click CloudFormation Method (Fastest & Easiest)](#3-method-1-the-1-click-cloudformation-method-fastest--easiest)
4. [Method 2: Launching Manually via AWS Console](#4-method-2-launching-manually-via-aws-console)
5. [Connecting to Your Server & Running the Setup Script](#5-connecting-to-your-server--running-the-setup-script)
6. [Accessing Your Platform in Your Browser](#6-accessing-your-platform-in-your-browser)
7. [Adding a Custom Domain & Free HTTPS (SSL)](#7-adding-a-custom-domain--free-https-ssl)
8. [Helpful Commands & Day-to-Day Maintenance](#8-helpful-commands--day-to-day-maintenance)
9. [Estimated AWS Cost Breakdown](#9-estimated-aws-cost-breakdown)

---

## 1. How ConnectedOps Runs on AWS

ConnectedOps is packaged using **Docker containers**. When deployed to AWS:
- **`connectedops-web`** runs the web portal and live dashboards on port **`5001`**.
- **`connectedops-api`** runs the REST API and Swagger documentation on port **`5000`**.
- **`sql-server`** runs Microsoft SQL Server 2022 on port **`1433`** with persistent cloud storage.

All three containers communicate privately and securely inside the server.

---

## 2. What You Need Before Starting

1. **An AWS Account**: If you don't have one yet:
   - Go to [https://aws.amazon.com/](https://aws.amazon.com/) and click **"Create an AWS Account"**.
   - Follow the prompts (requires an email and a payment card for identity verification).
   - Log in to the **AWS Management Console** as the **Root User**.

2. **Select Your Nearest Region**:
   - In the top-right corner of the AWS Console (next to your name), choose your region (e.g., *US East (N. Virginia) `us-east-1`*, *Europe (Frankfurt) `eu-central-1`*, or whichever is closest to you).

---

## 3. Method 1: The 1-Click CloudFormation Method (Fastest & Easiest)

We have provided a template file [`aws/cloudformation.yml`](aws/cloudformation.yml) that creates the virtual server, network, firewall rules, and static IP automatically.

### Step 1: Open CloudFormation in AWS
1. In the search bar at the very top of the AWS Console, type **CloudFormation** and click on it.
2. Click the orange button **"Create stack"** (select **"With new resources (standard)"**).

### Step 2: Upload the Template
1. Under *Prerequisite - Prepare template*, choose **"Template is ready"**.
2. Under *Template source*, select **"Upload a template file"**.
3. Click **"Choose file"** and select `aws/cloudformation.yml` from this project folder.
4. Click **"Next"**.

### Step 3: Configure Parameters
1. **Stack name**: Type `ConnectedOps-Production`.
2. **InstanceType**: Leave as `t3.large` (recommended) or choose `t3.medium`.
3. **AdminCidr**: Leave as `0.0.0.0/0` (or type your home/office IP followed by `/32`).
4. **GitRepositoryUrl**: If your code is on a public GitHub repo, paste the URL here. If private or not yet uploaded, leave it blank!
5. Click **"Next"**, click **"Next"** again on the Configure stack options page.

### Step 4: Create Stack
1. Scroll down to the bottom and check the box:
   > *"I acknowledge that AWS CloudFormation might create IAM resources with custom names."*
2. Click **"Submit"**.
3. CloudFormation will display `CREATE_IN_PROGRESS`. Wait 2 to 3 minutes until it says `CREATE_COMPLETE` in green.
4. Click on the **"Outputs"** tab. You will see:
   - **`ServerStaticIp`**: Your server's permanent public IP address.
   - **`WebPortalUrl`**: `http://<YOUR_IP>:5001`
   - **`ApiGatewayUrl`**: `http://<YOUR_IP>:5000/swagger`

---

## 4. Method 2: Launching Manually via AWS Console

If you prefer to click through the EC2 interface yourself:

1. In the top search bar, type **EC2** and press Enter.
2. Click the orange **"Launch instance"** button.
3. Fill in the following:
   - **Name**: `ConnectedOps-Server`
   - **Application and OS Images**: Choose **Amazon Linux** (Amazon Linux 2023 AMI is selected by default).
   - **Instance type**: Select **`t3.large`** (2 vCPU, 8 GB Memory — ideal for SQL Server + .NET).
   - **Key pair (login)**: Choose *Proceed without a key pair* (you can use AWS Web Console terminal), OR click *Create new key pair* to download an SSH key.
   - **Network settings**: Click *Edit*:
     - Check **"Allow SSH traffic from anywhere"**.
     - Check **"Allow HTTP traffic from the internet"**.
     - Check **"Allow HTTPS traffic from the internet"**.
     - Click **"Add security group rule"**:
       - Type: Custom TCP, Port: **`5001`**, Source: Anywhere (`0.0.0.0/0`) — *Web Portal*.
     - Click **"Add security group rule"**:
       - Type: Custom TCP, Port: **`5000`**, Source: Anywhere (`0.0.0.0/0`) — *REST API*.
   - **Configure storage**: Change **8 GiB** to **`40 GiB`** (gp3 SSD).
4. Click **"Launch instance"**.

---

## 5. Connecting to Your Server & Running the Setup Script

Once your EC2 server is running:

### Step 1: Connect via Browser (No SSH client required!)
1. In the AWS Console, go to **EC2 > Instances**.
2. Select your `ConnectedOps-Server` checkbox and click the **"Connect"** button at the top.
3. Keep the default tab **"EC2 Instance Connect"** selected.
4. Click the orange **"Connect"** button at the bottom.
5. A black terminal window opens right inside your browser!

### Step 2: Upload or Clone the Code
If your code is on GitHub:
```bash
git clone <YOUR_GITHUB_REPO_URL> connectedops
cd connectedops
```
*If you uploaded your code via SCP/SFTP or ZIP, simply `cd` into the project directory.*

### Step 3: Run the Automated Setup Script
Run this single command:
```bash
sudo bash aws/setup-ec2.sh
```

### What the script does automatically:
1. Installs Docker Engine, Git, and utilities.
2. Generates secure, randomized production passwords for SQL Server and JWT authentication in `.env`.
3. Sets up a background system service (`connectedops.service`) so your platform starts automatically on system reboots.
4. Builds the .NET 10 Web and API containers.
5. Starts SQL Server, API, and Web containers.
6. Prints your public URLs on the screen!

---

## 6. Accessing Your Platform in Your Browser

Once the setup script finishes, open your browser and navigate to:

| Service | URL | Description |
| :--- | :--- | :--- |
| **Fleet Management Portal** | `http://<YOUR_SERVER_IP>:5001` | Complete enterprise UI, live telematics, maps, and fleet management. |
| **REST API Gateway** | `http://<YOUR_SERVER_IP>:5000/swagger` | Interactive OpenAPI / Swagger documentation for all endpoints. |
| **Real-Time SignalR** | `ws://<YOUR_SERVER_IP>:5001/hubs/fleet` | WebSocket live streaming endpoint for telematics. |

> **Tip**: You can seed the full 20-phase enterprise demo data anytime by clicking the **"Seed All 20 Phases (Apex Global)"** button in the Web Portal under `/Demo/FleetSimulator`.

---

## 7. Adding a Custom Domain & Free HTTPS (SSL)

When you are ready to point your company domain (e.g., `fleet.yourcompany.com`) to ConnectedOps:

### Step 1: Create a DNS A Record
Go to your domain provider (GoDaddy, Namecheap, Route 53, Cloudflare) and create an **A Record**:
- **Host / Name**: `fleet` (or `@` for root domain)
- **Points to / Value**: `<YOUR_SERVER_IP>`
- **TTL**: Auto or 300 seconds.

### Step 2: Run the SSL Automation Script
In your server terminal, run:
```bash
sudo bash aws/enable-ssl.sh fleet.yourcompany.com admin@yourcompany.com
```
The script automatically:
- Installs Nginx and Let's Encrypt Certbot.
- Configures reverse proxying from ports 80/443 directly to ConnectedOps.
- Obtains and installs a trusted SSL certificate.
- Sets up automatic certificate renewal.

Now you can access your platform at: **`https://fleet.yourcompany.com`**!

---

## 8. Helpful Commands & Day-to-Day Maintenance

All commands can be run from the `/opt/connectedops` directory:

| Action | Command |
| :--- | :--- |
| **View live logs of all containers** | `docker compose logs -f` |
| **View logs for Web only** | `docker compose logs -f connectedops-web` |
| **View logs for API only** | `docker compose logs -f connectedops-api` |
| **View logs for Database** | `docker compose logs -f sql-server` |
| **Check container health status** | `docker compose ps` |
| **Restart the whole platform** | `sudo systemctl restart connectedops` |
| **Stop the platform** | `sudo systemctl stop connectedops` |
| **Update code from GitHub** | `git pull && docker compose build && docker compose up -d` |

---

## 9. Estimated AWS Cost Breakdown

Running ConnectedOps on AWS is economical:

| Resource | Configuration | Estimated Cost |
| :--- | :--- | :--- |
| **EC2 Virtual Machine** | `t3.large` (2 vCPU, 8 GB RAM) | ~$25.00 / month |
| **Storage (EBS gp3 SSD)** | 40 GB SSD storage | ~$3.20 / month |
| **Elastic IP** | 1 static public IPv4 address | ~$3.60 / month |
| **Data Transfer** | Normal web usage | ~$1.00 - $2.00 / month |
| **TOTAL** | | **~$32.00 - $34.00 / month** |

> **Cost-Saving Tip**: If you are testing or doing a demo and don't need the server running 24/7, you can **Stop** the instance in the AWS Console whenever you are not using it. You only pay for compute hours while the instance is in the *Running* state!
