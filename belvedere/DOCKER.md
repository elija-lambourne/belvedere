# Docker Setup for Belvedere Frontend

This project is configured to run in Docker using Docker Compose. Follow the instructions below to build and run the application.

## Prerequisites

- Docker installed ([download](https://www.docker.com/products/docker-desktop))
- Docker Compose installed (usually comes with Docker Desktop)

## Quick Start

### 1. Using Docker Compose (Recommended)

The easiest way to get started is using Docker Compose:

```bash
# Build and start the container
docker-compose up --build

# Run in detached mode (background)
docker-compose up -d --build

# View logs
docker-compose logs -f frontend

# Stop the container
docker-compose down
```

The application will be available at `http://localhost` (or the port specified in `.env`)

### 2. Building the Docker Image Manually

```bash
# Build the image
docker build -t belvedere:latest .

# Run the container
docker run -p 80:80 belvedere:latest

# Run with environment variables
docker run -p 80:80 \
  -e VITE_APP_TITLE="My Photo App" \
  -e VITE_APP_NAME="Photography" \
  belvedere:latest
```

## Environment Variables

Environment variables are configured using the `VITE_` prefix for build-time variables. These are embedded into the JavaScript bundle during the build process.

### Available Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_APP_TITLE` | `Belvedere` | Application page title |
| `VITE_APP_NAME` | `Belvedere Photography` | Application display name |
| `PORT` | `80` | Port to run the nginx server on |

### Setting Environment Variables

#### Using `.env` file

1. Copy `.env.example` to `.env`:
```bash
cp .env.example .env
```

2. Edit `.env` with your values:
```env
VITE_APP_TITLE=My App
VITE_APP_NAME=My Photography App
PORT=8080
```

3. Run with Docker Compose:
```bash
docker-compose up --build
```

#### Using command line

```bash
docker-compose -e VITE_APP_TITLE="My App" up --build
```

Or with manual Docker:

```bash
docker build \
  --build-arg VITE_APP_TITLE="My App" \
  --build-arg VITE_APP_NAME="My Photography" \
  -t belvedere:latest .

docker run -p 80:80 belvedere:latest
```

## Docker Compose Configuration

The `docker-compose.yml` includes:

- ✅ Multi-stage build optimization
- ✅ Environment variable support
- ✅ Health checks
- ✅ Volume mounting for development (optional)
- ✅ Automatic restart policy
- ✅ Security headers via Nginx

## Production Considerations

### Custom Domain/Port

Edit your `.env` file:

```env
PORT=3000
VITE_APP_TITLE=Production Title
```

### SSL/HTTPS

For production with HTTPS, you can use a reverse proxy or modify the Nginx configuration:

```nginx
server {
    listen 443 ssl http2;
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    
    # ... rest of configuration
}

server {
    listen 80;
    return 301 https://$server_name$request_uri;
}
```

### Docker Registry Push

```bash
# Tag the image
docker tag belvedere:latest myregistry.azurecr.io/belvedere:latest

# Push to registry
docker push myregistry.azurecr.io/belvedere:latest
```

## Troubleshooting

### Port already in use

If port 80 is already in use, modify `.env`:

```env
PORT=8080
```

Then access at `http://localhost:8080`

### Build fails

Clear Docker cache and rebuild:

```bash
docker-compose down -v
docker-compose build --no-cache
docker-compose up
```

### See container logs

```bash
docker-compose logs frontend -f
```

## How Environment Variables Work

The Vite build process bakes environment variables (prefixed with `VITE_`) into the JavaScript bundle during build time. This means:

1. Variables declared with `VITE_` prefix are embedded in the built JavaScript
2. They're accessed in code via `import.meta.env.VITE_VARIABLE_NAME`
3. Changes to env variables require a rebuild

Example usage in the app:

```typescript
const appTitle = import.meta.env.VITE_APP_TITLE || 'Default Title'
document.title = appTitle
```

## File Structure

```
├── Dockerfile           # Multi-stage Docker build configuration
├── docker-compose.yml   # Docker Compose orchestration
├── nginx.conf          # Nginx web server configuration
├── .dockerignore        # Files to exclude from Docker build
├── .env.example         # Example environment variables
└── src/
    └── App.tsx        # Uses environment variables
```

## Next Steps

- Add more `VITE_` prefixed variables as needed
- Configure SSL certificates for production
- Set up a CI/CD pipeline to build and push images
- Monitor container health and performance

