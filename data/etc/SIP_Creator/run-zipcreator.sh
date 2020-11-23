#!/bin/bash

trap "echo EXIT >&2; exit 66" err

WHEREAMI=$(dirname $(realpath $0))
if [ -z "$JAVA_HOME" ]
then
   export JAVA_HOME=/usr/local/openjdk-8 # Hogere versie dan Java 8 niet ondersteund door SIP Creator
fi

if [ -z "$2" ]
then
    echo "$0 vereist 1. een input-directory met een archiefstructuur, 2. een leeg output-directory voor de zip, 3. optioneel Preservica-reference". >&2
    exit 1
fi

shopt -s extglob

INPUTFOLDER=$1
OUTPUTFOLDER=$2
PRESERVICA_REFERENCE=$3
OUTPUTFOLDER_BASE=`basename "$OUTPUTFOLDER"`

if [ ! -d "$OUTPUTFOLDER" ]
then
   mkdir "$OUTPUTFOLDER"
fi

function doIt {
    outputfilescount=$(find "$OUTPUTFOLDER" -type f | grep -v '[.]log$' | wc -l)
    if [ $outputfilescount -ne 0 ]
    then
       echo "Uitvoerfolder $OUTPUTFOLDER bevat $outputfilescount files, gestopt" >&2
       exit 1
    fi
    
    # Switches conform https://noordhollandsarchief.sharepoint.com/:x:/r/sites/ImplementatiePreserveringsvoorziening/Gedeelde%20documenten/General/2.%20Technische%20documentatie/Pre-ingest/CMD%20SIP%20creator%20settings.xlsx?d=w9e29f946e0624d7db2d1efeca5f3525b&csf=1&web=1&e=Z7CTt7
    # Let op: -excludedFileNames kan nuttig zijn, maar ondersteunt geen wildcards.
    if [ -z "$PRESERVICA_REFERENCE" ]
    then
        "$WHEREAMI/createsip" \
            -input "$INPUTFOLDER" \
            -output "$OUTPUTFOLDER" \
            -status NEW \
            -securitytag open \
            -export \
            -sha512
    else
        "$WHEREAMI/createsip" \
            -input "$INPUTFOLDER" \
            -output "$OUTPUTFOLDER" \
            -status SAME \
            -colref "$PRESERVICA_REFERENCE" \
            -securitytag open \
            -export \
            -sha512
    fi
    
    cd "$OUTPUTFOLDER"
    zip -r "$OUTPUTFOLDER/$OUTPUTFOLDER_BASE.zip" !(*.log)
    
    rm -Rf !(*.zip|*.log)
}

doIt $1 $2 >"$OUTPUTFOLDER/$OUTPUTFOLDER_BASE.log" 2>&1

