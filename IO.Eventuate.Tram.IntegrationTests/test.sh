#!/usr/bin/env bash

set -e
set -x

docker-compose down
docker-compose up -d mssql

# Wait for MSSQL to start up
sleep 40s

docker-compose up --exit-code-from dbsetup dbsetup
docker-compose up -d zookeeper
docker-compose up -d kafka
docker-compose up -d cdcservice1
docker-compose up -d cdcservice2
                            
# Wait for dockers to start up
sleep 40s

docker-compose run --rm eventuatetramtests
docker stats --no-stream --all