# Belvedere backend

This folder contains the ASP.NET Core backend and a Docker Compose setup for local development.

## Run with Docker Compose

```bash
docker compose up --build
```

## Run for active development (only required when modifying code)
The docker services can be run separately from the backend, therefore enabling in-IDE running of the backend and debugging.
It is recommended to run these services in this order and opening a new terminal for each for observability.

```bash
docker compose up postgres
```
after it finished setting up also run the migrations service
```bash
docker compose up migrations
```
Now you can run minio for the S3 service:

```bash
docker compose up minio
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
