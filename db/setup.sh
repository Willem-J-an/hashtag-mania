#/bin/bash

(docker ps | grep twitter-db ) | cut -c 1-12
docker run --name twitter-db -e POSTGRES_PASSWORD=mysecretpassword -d postgres

docker exec -it some-postgres bash

su postgres