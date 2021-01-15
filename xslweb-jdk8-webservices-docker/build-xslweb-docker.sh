#!/bin/bash

WHEREAMI=$(dirname $(realpath $0))
XSLWEB_SRC=$WHEREAMI/xslweb-source/xslweb
cd "$XSLWEB_SRC"

# Uncomment dit als je XSLWeb moet hercompileren
#mvn clean install

WAR_COUNT=`find target -name '*.war' | wc -l`

if [ $WAR_COUNT != 1 ]
then
   echo Aantal gevonden war-files is niet 1 maar $WAR_COUNT >&2
   exit 1
fi

WAR_FILE=`find target -name '*.war'`

# Docker wil alle files in de folder hebben met de dockerfile:
cp "$WAR_FILE" "$WHEREAMI/xslweb.war"
cp `which zip` "$WHEREAMI"

cd "$WHEREAMI"

docker build --tag noordhollandsarchief/xslweb:development .

rm "$WHEREAMI"/xslweb.war
rm "$WHEREAMI"/zip