# Belvedere backend

This folder contains the ASP.NET Core backend and a Docker Compose setup for local development.

## Run with Docker Compose

```bash
docker compose up --build
```

The stack starts:

- `postgres` on the port configured in `.env` (`5432` by default)
- `backend` on `http://localhost:8080`

## Environment variables

Docker Compose reads the `.env` file and injects the values into the backend container as normal environment variables.

Important settings:

- `ConnectionStrings__Postgres`
- `General__ClientOrigin`
- `Keycloak__Authority`
- `Keycloak__ClientId`
- `Keycloak__ClientSecret`
- `Keycloak__RequireHttpsMetadata`

## Notes

- The backend does not parse `.env` directly.
- `docker-compose.yml` injects the PostgreSQL connection string and Keycloak settings into the backend container.
