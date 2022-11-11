docker network create platform-example-app-network

SET UseDbType=Postgres
docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example kill
docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example rm -f
docker volume prune -f
docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example build sql-data mongo-data postgres-sql rabbitmq redis-cache text-snippet-api text-snippet-webspa
docker-compose -f platform-example-app.docker-compose.yml -f platform-example-app.docker-compose.override.yml -p easyplatform-example up sql-data mongo-data postgres-sql rabbitmq redis-cache text-snippet-api text-snippet-webspa --remove-orphans --detach
pause
