#!/usr/bin/env bash

VERSION=$1
[ -z "$VERSION" ] && echo "No version specified" && exit 1
echo "Writing docker image of API server version $VERSION to Docker Hub"

docker build --no-cache -t qumranica/sqe-http-api:latest -f ./docker/Dockerfile .
docker tag qumranica/sqe-http-api:latest qumranica/sqe-http-api:"$VERSION"
docker push qumranica/sqe-http-api

echo "Fineshed writing docker image of API server version $VERSION to Docker Hub"