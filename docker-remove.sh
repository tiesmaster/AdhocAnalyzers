#!/bin/bash

docker rm -f $(docker ps -a -q --filter ancestor=adhocanalyzers:adhocanalyzers)
docker rmi $(docker images adhocanalyzers -q)