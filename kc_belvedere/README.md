# Keycloak Setup for Belvedere

A containerized Keycloak authentication server setup for the Belvedere application.

## Overview

This directory contains a complete Docker Compose setup for running Keycloak with PostgreSQL as the backing database. It includes:

- **Keycloak**: Latest version authentication server
- **PostgreSQL**: Database for storing Keycloak data
- **Pre-configured Realm**: "belvedere" realm with initial configuration
- **Test User**: Pre-configured test user for development

## Prerequisites

- Docker
- Docker Compose
- At least 2GB of available RAM

## Quick Start

### 1. Environment Configuration

The `.env` file contains all configuration variables. Key settings:

- **Admin Credentials**: 
  - Username: `admin`
  - Password: `admin_password`
- **Database**: PostgreSQL with username/password `keycloak:keycloak_password`
- **Access URL**: `http://localhost:8080`

### 2. Start the Services

To start Keycloak and PostgreSQL:

```bash
cd kc_belvedere
docker-compose up -d
```

To follow logs:

```bash
docker-compose logs -f keycloak
```

### 3. Access Keycloak

Once the container is healthy:

- **Admin Console**: `http://localhost:8080/admin`
- **Default Credentials**: `admin / admin_password`
- **Realm**: belvedere

### 4. Test User

A test user is pre-configured for development:

- **Username**: `testuser`
- **Email**: `testuser@example.com`
- **Password**: `Test@1234`

## Configuration Details

### Realm: `belvedere`

The pre-configured "belvedere" realm includes:

- **Client**: `belvedere-client`
  - Client Secret: `your-client-secret-here` (change in production)
  - Redirect URIs: `http://localhost:5173/*`, `http://localhost:3000/*`
  - Web Origins: localhost:5173, localhost:3000, localhost:8080

- **Roles**: 
  - `user`: Standard user role
  - `admin`: Administrator role (includes user role)

- **Password Policy**: 
  - Minimum 8 characters
  - Force password change after 365 days
  - Username cannot be the password

### Database

PostgreSQL is configured with:

- **Database**: keycloak
- **User**: keycloak
- **Password**: keycloak_password
- **Port**: 5432
- **Volume**: `postgres_data` for persistence

## Environment Variables

See `.env` file for all available configuration options. Key variables:

```env
KC_ADMIN=admin
KC_ADMIN_PASSWORD=admin_password
KC_HOSTNAME=localhost
KC_HOSTNAME_PORT=8080
```

## File Structure

```
kc_belvedere/
├── docker-compose.yml          # Docker Compose configuration
├── .env                         # Environment variables
├── realms/
│   └── belvedere-realm.json    # Pre-configured realm
└── README.md                    # This file
```

## Stopping Services

To stop the services:

```bash
docker-compose down
```

To stop and remove all data:

```bash
docker-compose down -v
```

## Common Commands

### View logs

```bash
docker-compose logs -f keycloak
docker-compose logs -f postgres
```

### Access container shell

```bash
docker-compose exec keycloak /bin/bash
docker-compose exec postgres psql -U keycloak -d keycloak
```

### Restart services

```bash
docker-compose restart keycloak
docker-compose restart postgres
```

## Customization

### Change Realm Configuration

Edit `realms/belvedere-realm.json` to customize:
- Realm name and settings
- Client configurations
- Users and roles
- Themes

### Change Credentials

Update credentials in `.env`:
- Keycloak admin password: `KC_ADMIN_PASSWORD`
- Database password: `POSTGRES_PASSWORD`

### Adjust Ports

Modify `docker-compose.yml`:
- Keycloak default: `8080:8080`
- PostgreSQL default: `5432:5432`

## Troubleshooting

### Container won't start

Check logs:
```bash
docker-compose logs keycloak
```

### Database connection issues

Ensure PostgreSQL is healthy:
```bash
docker-compose logs postgres
```

### Port already in use

Change ports in `docker-compose.yml` and update `KC_HOSTNAME_PORT` in `.env`

## Security Notes

⚠️ **Important for Production:**

1. Change all default passwords in `.env`
2. Set `KC_HOSTNAME_STRICT_HTTPS=true` in production
3. Generate a secure client secret for `belvedere-client`
4. Configure SSL/TLS certificates
5. Enable HTTPS redirect
6. Review Keycloak security guidelines

## Integration with Belvedere App

The Belvedere application should be configured to use:

- **Authorization Server**: `http://localhost:8080`
- **Realm**: `belvedere`
- **Client ID**: `belvedere-client`
- **Client Secret**: (as configured in realm JSON)
- **Token Endpoint**: `http://localhost:8080/realms/belvedere/protocol/openid-connect/token`
- **Authorization Endpoint**: `http://localhost:8080/realms/belvedere/protocol/openid-connect/auth`

## References

- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [Keycloak Docker Images](https://quay.io/keycloak/keycloak)
- [OpenID Connect Protocol](https://openid.net/connect/)

## License

Same as the parent Belvedere project.

