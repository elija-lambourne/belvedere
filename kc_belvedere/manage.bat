@echo off
REM Helper script to manage Keycloak services on Windows

setlocal enabledelayedexpansion

if "%1%"=="" goto help
if "%1%"=="help" goto help
if "%1%"=="up" goto up
if "%1%"=="down" goto down
if "%1%"=="down-v" goto down_clean
if "%1%"=="logs" goto logs
if "%1%"=="logs-db" goto logs_db
if "%1%"=="logs-all" goto logs_all
if "%1%"=="status" goto status
if "%1%"=="restart" goto restart
if "%1%"=="health" goto health

echo Unknown command: %1%
goto help

:help
echo Keycloak Management Script
echo.
echo Usage: %0% [command]
echo.
echo Commands:
echo   up              Start Keycloak and PostgreSQL services
echo   down            Stop all services
echo   down-v          Stop all services and remove volumes
echo   logs            View Keycloak logs
echo   logs-db         View PostgreSQL logs
echo   logs-all        View all service logs
echo   status          Show service status
echo   restart         Restart all services
echo   health          Check service health
echo   help            Show this help message
echo.
goto end

:up
echo Starting Keycloak and PostgreSQL...
docker-compose up -d
if %ERRORLEVEL% EQU 0 (
    echo Services started!
    echo Keycloak will be available at: http://localhost:8080
    echo Admin console: http://localhost:8080/admin
) else (
    echo Failed to start services
    exit /b 1
)
goto end

:down
echo Stopping services...
docker-compose down
echo Services stopped!
goto end

:down_clean
echo Stopping services and removing volumes...
docker-compose down -v
echo Services stopped and volumes removed!
goto end

:logs
docker-compose logs -f keycloak
goto end

:logs_db
docker-compose logs -f postgres
goto end

:logs_all
docker-compose logs -f
goto end

:status
echo Service Status:
docker-compose ps
goto end

:restart
echo Restarting services...
docker-compose restart
echo Services restarted!
goto end

:health
echo Checking service health...
echo.
echo Trying to reach Keycloak...
curl -s http://localhost:8080/health/ready > nul
if %ERRORLEVEL% EQU 0 (
    echo Keycloak is ready
) else (
    echo Keycloak is not ready
)
goto end

:end
endlocal

