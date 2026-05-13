# Backend Complete Setup Instructions
> [!WARNING]
> This documentation is AI generated (GPT-5.2 Codex). Take instructions with a grain of salt and **do NOT trust**
> the instructions blindy. Human written and verified documentation will be released at a later date.


This folder contains all backend services for Belvedere:
- ASP.NET Core API (Port 5001)
- PostgreSQL Database (Port 5432)
- Garage S3 Object Storage (Port 3900)

## Prerequisites

- Docker & Docker Compose installed
- Keycloak instance for OIDC auth (or use provided kc_belvedere)
- .env file configured (see .env.example)

## Getting Started

### 1. Copy environment template

```bash
cp .env.example .env
```

### 2. Configure environment variables

Edit `.env` and set:

**Database:**
```
POSTGRES_USER=belvedere
POSTGRES_PASSWORD=developer_password  # Change in production
POSTGRES_DB=belvedere
POSTGRES_PORT=5432
```

**Garage S3 Storage:**
```
GARAGE_RPC_SECRET=dev_rpc_secret  # Change in production
GARAGE_ADMIN_TOKEN=dev_admin_token  # Change in production
GARAGE_S3_PORT=3900
GARAGE_ADMIN_PORT=3903
Storage__AccessKey=belvedere_access_key
Storage__SecretKey=belvedere_secret_key
```

**Keycloak Authentication:**
```
Keycloak__Authority=http://keycloak:8080/realms/belvedere
Keycloak__ClientId=belvedere-backend
Keycloak__ClientSecret=your_client_secret_here
Keycloak__RequireHttpsMetadata=false  # Set to true in production
```

**General Settings:**
```
General__ClientOrigin=http://localhost:5173  # Frontend URL
API_PORT=5001
ASPNETCORE_ENVIRONMENT=Development
```

### 3. Start services

```bash
# With Garage (recommended):
docker-compose -f docker-compose.garage.yml up -d

# Or use original docker-compose (you must have S3 configured elsewhere):
docker-compose up -d
```

### 4. Verify services

```bash
docker-compose -f docker-compose.garage.yml ps
```

Expected output:
```
NAME                    SERVICE             STATUS              PORTS
belvedere-postgres      postgres            running (healthy)   0.0.0.0:5432->5432/tcp
belvedere-garage        garage              running (healthy)   0.0.0.0:3900->3900/tcp, 0.0.0.0:3903->3903/tcp, 0.0.0.0:3904->3904/tcp
belvedere-backend       backend             running             0.0.0.0:5001->8080/tcp
```

### 5. Initialize database

```bash
# Run migrations (backend will do this on startup)
# Check logs to confirm:
docker-compose -f docker-compose.garage.yml logs backend
```

Look for: "Database initialized" or "Migrations applied"

### 6. Create Garage bucket and credentials

```bash
# Access Garage admin panel
# Browser: http://localhost:3903
# Admin Token: value from your .env GARAGE_ADMIN_TOKEN

# Or use API directly to create bucket:
curl -X POST http://localhost:3903/v0/admin/buckets \
  -H "Authorization: Bearer your_admin_token" \
  -H "Content-Type: application/json" \
  -d '{"name":"belvedere"}'
```

### 7. Create S3 access credentials

Via Garage UI:
1. Go to http://localhost:3905 (UI)
2. Create API key
3. Update `.env`:
   ```
   Storage__AccessKey=<key_id>
   Storage__SecretKey=<secret_key>
   ```
4. Restart backend: `docker-compose restart backend`

### 8. Test connectivity

```bash
# Test database connection
docker-compose exec backend \
  dotnet tool install -g dotnet-ef

# Test S3/Garage connection  
curl -v -u "belvedere_access_key:belvedere_secret_key" \
  http://localhost:3900/

# Test API
curl http://localhost:5001/health
```

## Configuration Files

### docker-compose.garage.yml

Complete stack with:
- PostgreSQL (database)
- Garage (S3 storage)
- Belvedere backend (API)

All services on dedicated Docker network.

### .env (not in repo, create from .env.example)

Environment variables for:
- Database credentials
- Garage storage settings
- Keycloak OIDC configuration
- API ports and client origin

## Development Workflow

### Update code and rebuild

```bash
# After code changes:
docker-compose -f docker-compose.garage.yml build backend
docker-compose -f docker-compose.garage.yml up -d
```

### View logs

```bash
# Backend logs
docker-compose -f docker-compose.garage.yml logs -f backend

# Database logs
docker-compose -f docker-compose.garage.yml logs -f postgres

# Garage logs
docker-compose -f docker-compose.garage.yml logs -f garage
```

### Access containers

```bash
# Backend shell
docker-compose exec backend /bin/sh

# Database psql
docker-compose exec postgres psql -U belvedere -d belvedere

# Garage metrics
curl http://localhost:3904/metrics
```

## Production Deployment

### Security Checklist

- [ ] Change all default passwords in `.env`
  - POSTGRES_PASSWORD
  - GARAGE_RPC_SECRET
  - GARAGE_ADMIN_TOKEN
  - Keycloak__ClientSecret

- [ ] Set `Keycloak__RequireHttpsMetadata=true`
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure real domain for `General__ClientOrigin`
- [ ] Use HTTPS everywhere (reverse proxy like nginx/traefik)
- [ ] Set up persistent volume backups
- [ ] Enable Garage metrics collection and alerting
- [ ] Configure database backups

### Reverse Proxy Example (nginx)

```nginx
upstream backend {
    server backend:8080;
}

server {
    listen 443 ssl http2;
    server_name api.belvedere.example.com;
    
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    
    location / {
        proxy_pass http://backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
    }
}
```

### Database Backups

```bash
# Backup database
docker-compose exec postgres pg_dump -U belvedere belvedere > backup.sql

# Restore from backup
cat backup.sql | docker-compose exec -T postgres psql -U belvedere belvedere
```

### Garage Data Backups

```bash
# Stop Garage
docker-compose stop garage

# Backup volume
docker run --rm -v belvedere_garage_data:/data -v $(pwd):/backup \
  alpine tar czf /backup/garage-backup.tar.gz -C / data

# Restart Garage
docker-compose start garage
```

## Troubleshooting

### Backend fails to start

```bash
# Check logs for error
docker-compose logs backend

# Common issues:
# - Database not healthy: Check postgres logs
# - Garage not ready: Wait for healthcheck
# - Missing env variables: Verify .env file
# - Port already in use: Change API_PORT in .env
```

### Database connection error

```bash
# Verify database is running and healthy
docker-compose ps postgres

# Check connection string in .env
# Format: Host=postgres;Port=5432;Database=belvedere;Username=belvedere;Password=xxx

# Test directly
docker-compose exec postgres psql -U belvedere -d belvedere -c "SELECT 1"
```

### S3/Garage connection error

```bash
# Check Garage is running and healthy
docker-compose ps garage

# Test S3 API
curl -v http://localhost:3900/

# Verify credentials in backend env
docker-compose exec backend env | grep Storage__

# Check backend logs for S3 errors
docker-compose logs -f backend | grep -i "storage\|s3\|garage"
```

### CSRF token errors

This is normal behavior - it means CSRF protection is working.
- Frontend must extract token from `__Host-belvedere-session` cookie
- Include in X-XSRF-TOKEN header for POST/PUT/DELETE requests
- See frontend integration guide

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                  Docker Network                         │
│                  belvedere-network                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │   Belvedere     │  │ PostgreSQL   │  │  Garage    │ │
│  │   Backend API   │→─│   Database   │  │  S3 Store  │ │
│  │  Port 8080      │  │  Port 5432   │  │ Port 3900  │ │
│  └─────────────────┘  └──────────────┘  └────────────┘ │
│          ↓                 ↑                                │
│   Exposed: 5001      Volume: postgres_data  Volume: garage_data
│                                                             │
└─────────────────────────────────────────────────────────┘
```

## Support & Documentation

- **Belvedere README:** ../README.md
- **Image Storage Audit:** ./IMAGE_STORAGE_AUDIT.md
- **Workflow Report:** ./BACKEND_WORKFLOW_REPORT.md
- **Garage Setup:** ../garage/README.md
- **API Documentation:** http://localhost:5001/swagger (when enabled)
- **Keycloak Docs:** https://www.keycloak.org/documentation.html

