#!/usr/bin/env bash


################################################################################
# This shell script makes it easy to build the SQE API Docker image and to     #
# upload it to the DockerHub container repository that we use. It can be run   #
# with `./write_to_docker_hub.sh v1.1.1`, where the current version number is  #
# specified following the executable name. You will need to be locally logged  #
# in to DockerHub and also have write access to the qumranica/sqe-http-api     #
# repo.     TODO: maybe move to releasing Docker images on GitHub              #
################################################################################


VERSION=$1
[ -z "$VERSION" ] && echo "No version specified" && exit 1
echo "Writing docker image of API server version $VERSION to Docker Hub"

docker build --no-cache -t qumranica/sqe-http-api:latest -f ./docker/Dockerfile .
docker tag qumranica/sqe-http-api:latest qumranica/sqe-http-api:"$VERSION"
docker push qumranica/sqe-http-api:latest
docker push qumranica/sqe-http-api:"$VERSION"

echo "Fineshed writing docker image of API server version $VERSION to Docker Hub"
