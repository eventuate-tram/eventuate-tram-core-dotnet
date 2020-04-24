#!/usr/bin/env bash

set -e
set -x

docker-compose down
docker-compose up -d mssql

# Wait for MSSQL to start up
sleep 40s

docker-compose run --rm dbsetup
docker-compose up -d zookeeper
docker-compose up -d kafka
docker-compose up -d cdcservice
                            
# Wait for docker containers to start up
sleep 40s

docker-compose run --rm eventuatetramtests
docker stats --no-stream --all