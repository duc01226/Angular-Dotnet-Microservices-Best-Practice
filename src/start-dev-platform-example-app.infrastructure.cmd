docker network create platform-example-app-network

docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example kill
docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example build sql-data mongo-data postgres-sql rabbitmq redis-cache
docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example up --remove-orphans --detach sql-data mongo-data postgres-sql rabbitmq redis-cache
pause
