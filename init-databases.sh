#!/bin/sh
#eseguito da Postgres SOLO al primo avvio (volume vuoto).
set -eu

create_service_db() {
  name="$1"
  password="$2"

  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<EOSQL
CREATE ROLE "$name" LOGIN PASSWORD '$password';
CREATE DATABASE "$name" OWNER "$name";
REVOKE ALL ON DATABASE "$name" FROM PUBLIC;
EOSQL

  echo "Database '$name' creato"
}

create_service_db identity      "$IDENTITY_DB_PASSWORD"
create_service_db subscriptions "$SUBSCRIPTIONS_DB_PASSWORD"
create_service_db media         "$MEDIA_DB_PASSWORD"