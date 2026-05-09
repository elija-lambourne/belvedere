# Optional Dockerfile for custom Keycloak image
# Use this if you need to add custom themes, extensions, or providers
# Otherwise, the docker-compose.yml uses the official Keycloak image

FROM quay.io/keycloak/keycloak:latest as builder

# Add any custom themes or providers here
# COPY ./themes/ /opt/keycloak/themes/
# COPY ./providers/ /opt/keycloak/providers/

ENV KC_DB=postgres

RUN /opt/keycloak/bin/kc.sh build --db=postgres

FROM quay.io/keycloak/keycloak:latest

COPY --from=builder /opt/keycloak /opt/keycloak

WORKDIR /opt/keycloak

ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]

