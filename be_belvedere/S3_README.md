# Garage S3 Object Storage Setup

> [!NOTE]
> **Docker services for Garage have been consolidated into `../be_belvedere/docker-compose.yml`**
> 
> The `garage` and `garage-ui` services now run on the unified `belvedere-network` alongside the PostgreSQL database, backend API, and migrations service.
> To start all services including Garage, use `docker-compose up` from the `be_belvedere` directory.
> See `../be_belvedere/.env.example` for complete environment variable configuration.

This documentation provides reference information about Garage, an S3-compatible object storage service for Belvedere.
> [!WARNING]
> This documentation is AI generated (GPT-5.2 Codex). Take instructions with a grain of salt and **do NOT trust**
> the instructions blindy. Human written and verified documentation will be released at a later date.

## Architecture

Garage is a lightweight, self-hosted object storage service that provides a complete S3 API compatible interface. It's perfect for Belvedere because:

- ✅ Full S3 API compatibility with the AWS SDK
- ✅ Self-hosted (no cloud vendor lock-in)
- ✅ Lightweight and performant
- ✅ Supports presigned URLs for temporary access
- ✅ Works seamlessly with the Belvedere backend storage layer

## Quick Start

### 1. Copy Environment Variables

```bash
cp .env.example .env
```

### 2. Start Garage

```bash
docker-compose up -d
```

### 3. Verify Services

```bash
docker-compose ps
```

You should see:
- `belvedere-garage` (S3 API on port 3900)
- `belvedere-garage-ui` (Admin UI on port 3905)

### 4. Create S3 Bucket and Access Keys

#### Option A: Via Garage UI (Recommended)

1. Open http://localhost:3905 in your browser
2. Enter the admin token from your `.env` file
3. Create a new bucket named `belvedere`
4. Create S3 API credentials
5. Note the Access Key and Secret Key

#### Option B: Via AWS CLI

```bash
# Install aws-cli if not already installed
pip install awscli

# Configure AWS CLI
aws configure --profile garage-local

# When prompted:
# AWS Access Key ID: [from Garage UI or first create]
# AWS Secret Access Key: [from Garage UI or first create]
# Default region name: us-east-1
# Default output format: json

# Create bucket
aws s3 --endpoint-url http://localhost:3900 --profile garage-local mb s3://belvedere

# List buckets
aws s3 --endpoint-url http://localhost:3900 --profile garage-local ls
```

#### Option C: Via curl (Manual API)

```bash
# Get admin status
curl -X GET \
  -H "Authorization: Bearer dev_admin_token_change_me_in_production" \
  http://localhost:3903/v0/status

# Create S3 key (see Garage API documentation for full details)
```

## Configuration

### Environment Variables

Key variables in `.env`:

| Variable | Purpose | Default |
|----------|---------|---------|
| `GARAGE_RPC_SECRET` | Internal Garage cluster secret | `insecure_dev_secret_change_me_in_production` |
| `GARAGE_ADMIN_TOKEN` | Admin API access token | `dev_admin_token_change_me_in_production` |
| `GARAGE_S3_PORT` | S3 API port (maps to core service) | `3900` |
| `Storage__ServiceUrl` | Backend S3 endpoint URL | `http://garage:3900` |
| `Storage__BucketName` | S3 bucket for photo storage | `belvedere` |
| `Storage__AccessKey` | S3 access key | Set via Garage UI |
| `Storage__SecretKey` | S3 secret key | Set via Garage UI |
| `Storage__ForcePathStyle` | **MUST be true for Garage** | `true` |

### Production Checklist

- [ ] Change `GARAGE_RPC_SECRET` to a cryptographically secure value
- [ ] Change `GARAGE_ADMIN_TOKEN` to a secure token
- [ ] Generate secure S3 API credentials (don't share with anyone)
- [ ] Set `Storage__AccessKey` and `Storage__SecretKey` in backend `.env`
- [ ] Use persistent volume mounted to `/data` for data durability
- [ ] Enable TLS/HTTPS on the reverse proxy (nginx/traefik)
- [ ] Test presigned URL generation from backend
- [ ] Backup Garage data regularly (volume contents)

## Integration with Belvedere Backend

The backend service automatically connects to Garage using environment variables.

### Backend Environment Variables

Add these to `be_belvedere/.env`:

```bash
Storage__ServiceUrl=http://garage:3900
Storage__BucketName=belvedere
Storage__AccessKey=<your-garage-access-key>
Storage__SecretKey=<your-garage-secret-key>
Storage__Region=us-east-1
Storage__ForcePathStyle=true
```

### Full Stack Docker Compose

See `../be_belvedere/docker-compose.full.yml` for a complete setup including:
- PostgreSQL database
- Garage storage
- Belvedere backend
- All networking configured

## Storage Key Structure

Photos are stored in Garage with the following key structure:

```
photos/YYYY/MM/<uuid>.<extension>
```

For example:
```
photos/2026/05/a1b2c3d4e5f6g7h8.jpg
photos/2026/05/x9y8z7w6v5u4t3s2_thumb.jpg
```

- Original photos: `photos/{year}/{month}/{uuid}.{ext}`
- Thumbnails: `photos/{year}/{month}/{uuid}_thumb.jpg`

## Backup and Recovery

### Backup Garage Data

```bash
# Stop Garage
docker-compose stop garage

# Backup the volume
docker run --rm -v belvedere_garage_data:/data -v $(pwd):/backup \
  alpine tar czf /backup/garage-backup.tar.gz -C / data

# Restart Garage
docker-compose start garage
```

### Restore Garage Data

```bash
# Stop Garage
docker-compose stop garage

# Restore the volume
docker run --rm -v belvedere_garage_data:/data -v $(pwd):/backup \
  alpine tar xzf /backup/garage-backup.tar.gz -C /

# Restart Garage
docker-compose start garage
```

## Monitoring

### Health Check

```bash
# Check S3 API health
curl http://localhost:3900

# Check admin API
curl -X GET \
  -H "Authorization: Bearer dev_admin_token_change_me_in_production" \
  http://localhost:3903/v0/status
```

### Logs

```bash
# View Garage logs
docker-compose logs -f garage

# View Garage UI logs
docker-compose logs -f garage-ui
```

### Metrics

Garage exposes metrics on port 3904 (Prometheus format):

```bash
curl http://localhost:3904/metrics
```

## Troubleshooting

### Garage not starting

```bash
# Check logs
docker-compose logs garage

# Verify environment variables
docker-compose config | grep GARAGE
```

### Cannot create bucket

- Verify admin token is correct
- Check that Garage service is healthy: `docker-compose ps`
- Ensure port 3903 (admin API) is not blocked

### Backend cannot connect to Garage

- Verify `Storage__ServiceUrl` is set to `http://garage:3900` (not `localhost`)
- Ensure both services are on the same Docker network
- Check backend logs for connection errors

### Presigned URLs not working

- Ensure `Storage__ForcePathStyle` is set to `true`
- Verify bucket exists and is accessible
- Check that credentials are correct

## Documentation Links

- [Garage Official Docs](https://garagehq.deuxfleurs.fr/)
- [Garage API Reference](https://garagehq.deuxfleurs.fr/documentation/api/)
- [AWS S3 API Reference](https://docs.aws.amazon.com/s3/latest/API/)

## Security Notes

🔒 **Important for Production:**

1. **Never use default tokens in production** - Generate cryptographically secure values
2. **Use TLS/HTTPS** - Always encrypt data in transit via reverse proxy
3. **Restrict access** - Only expose S3 API to the backend service
4. **Backup keys** - Store access keys securely (use secrets manager)
5. **Monitor access** - Enable Garage metrics and log monitoring
6. **Rotate credentials** - Regularly rotate S3 access keys

## Support

For issues with:
- **Garage storage:** See [Garage issues](https://github.com/deuxfleurs-org/garage/issues)
- **Belvedere backend:** Check logs: `docker-compose logs backend`
- **Docker networking:** Verify services can reach each other: `docker network inspect belvedere_belvedere-network`

