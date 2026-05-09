#!/bin/bash
# Helper script to manage Keycloak services

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Functions
show_help() {
    echo "Keycloak Management Script"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  up              Start Keycloak and PostgreSQL services"
    echo "  down            Stop all services"
    echo "  down-v          Stop all services and remove volumes"
    echo "  logs            View Keycloak logs"
    echo "  logs-db         View PostgreSQL logs"
    echo "  logs-all        View all service logs"
    echo "  status          Show service status"
    echo "  shell-kc        Access Keycloak container shell"
    echo "  shell-db        Access PostgreSQL container shell"
    echo "  restart         Restart all services"
    echo "  health          Check service health"
    echo "  help            Show this help message"
    echo ""
}

start_services() {
    echo -e "${GREEN}Starting Keycloak and PostgreSQL...${NC}"
    docker-compose up -d
    echo -e "${GREEN}Services started!${NC}"
    echo -e "${YELLOW}Keycloak will be available at: http://localhost:8080${NC}"
    echo -e "${YELLOW}Admin console: http://localhost:8080/admin${NC}"
}

stop_services() {
    echo -e "${YELLOW}Stopping services...${NC}"
    docker-compose down
    echo -e "${GREEN}Services stopped!${NC}"
}

stop_and_clean() {
    echo -e "${RED}Stopping services and removing volumes...${NC}"
    docker-compose down -v
    echo -e "${GREEN}Services stopped and volumes removed!${NC}"
}

view_logs() {
    docker-compose logs -f keycloak
}

view_db_logs() {
    docker-compose logs -f postgres
}

view_all_logs() {
    docker-compose logs -f
}

check_status() {
    echo -e "${YELLOW}Service Status:${NC}"
    docker-compose ps
}

access_keycloak_shell() {
    docker-compose exec keycloak /bin/bash
}

access_db_shell() {
    docker-compose exec postgres psql -U keycloak -d keycloak
}

restart_services() {
    echo -e "${YELLOW}Restarting services...${NC}"
    docker-compose restart
    echo -e "${GREEN}Services restarted!${NC}"
}

check_health() {
    echo -e "${YELLOW}Checking service health...${NC}"
    echo ""

    echo -e "${YELLOW}PostgreSQL Health:${NC}"
    docker-compose exec postgres pg_isready -U keycloak && echo -e "${GREEN}✓ PostgreSQL is ready${NC}" || echo -e "${RED}✗ PostgreSQL is not ready${NC}"

    echo ""
    echo -e "${YELLOW}Keycloak Health:${NC}"
    curl -s http://localhost:8080/health/ready > /dev/null && echo -e "${GREEN}✓ Keycloak is ready${NC}" || echo -e "${RED}✗ Keycloak is not ready${NC}"
}

# Main script
case "${1:-help}" in
    up)
        start_services
        ;;
    down)
        stop_services
        ;;
    down-v)
        stop_and_clean
        ;;
    logs)
        view_logs
        ;;
    logs-db)
        view_db_logs
        ;;
    logs-all)
        view_all_logs
        ;;
    status)
        check_status
        ;;
    shell-kc)
        access_keycloak_shell
        ;;
    shell-db)
        access_db_shell
        ;;
    restart)
        restart_services
        ;;
    health)
        check_health
        ;;
    help)
        show_help
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        show_help
        exit 1
        ;;
esac

