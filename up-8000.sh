#!/bin/bash

export DATAFOLDER=/home/pieter/noord-hollandsarchief/data

docker-compose -f docker-compose-poort-8000.yml up -d
