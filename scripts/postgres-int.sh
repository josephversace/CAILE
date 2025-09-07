#!/bin/bash
set -e

# Function to create a database if it doesn't exist
create_database() {
    local db_name=$1
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
        SELECT 'CREATE DATABASE $db_name'
        WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db_name')\gexec
EOSQL
}

# Create databases from environment variables
if [ -n "$POSTGRES_DB_CONFIG" ]; then
    create_database "$POSTGRES_DB_CONFIG"
fi
if [ -n "$POSTGRES_DB_MODEL" ]; then
    create_database "$POSTGRES_DB_MODEL"
fi
if [ -n "$POSTGRES_DB_AUDIT" ]; then
    create_database "$POSTGRES_DB_AUDIT"
fi
if [ -n "$POSTGRES_DB_AUTH" ]; then
    create_database "$POSTGRES_DB_AUTH"
fi
