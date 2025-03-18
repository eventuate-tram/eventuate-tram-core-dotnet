Set-Item -Path Env:CDC_SERVICE_DOCKER_VERSION -Value ("0.6.0.RELEASE")
Push-Location -Path IO.Eventuate.Tram.IntegrationTests

docker compose down --remove-orphans
docker compose up -d mssql
docker compose up -d zookeeper
docker compose up -d kafka

Start-Sleep -Seconds 40
docker stats --no-stream

docker compose run --rm kafka-setup
docker compose run --rm dbsetup
docker compose up -d cdcservice

docker compose up -d kafka-ui

Pop-Location