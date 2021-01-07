#!/bin/bash

if [ -z "$2" ]
then
    # Impossible to call moanAndDie: output directory has not yet been defined.
    echo "$0 vereist 1. een input-directory met een archiefstructuur, 2. de guid (corresponderend met de verwerkingsfolder), 3. optioneel Preservica-reference". >&2
    exit 1
fi

INPUTFOLDER=$1
OUTPUTFOLDER_BASE=sipresult
OUTPUTFOLDER=$INPUTFOLDER/../$OUTPUTFOLDER_BASE
GUID=$2
PRESERVICA_REFERENCE=$3

if [ -d "$OUTPUTFOLDER" ]
then
   rm -Rf "$OUTPUTFOLDER"
fi

mkdir "$OUTPUTFOLDER"

export LOGFILE=$OUTPUTFOLDER/$OUTPUTFOLDER_BASE.log
export ACTIONGUID=""

shopt -s extglob

function moanAndDie() {
   if [ -n "$1" ]
   then
       message=$1
   else
       message="An unknown error occurred"
   fi
   
   echo "$message" >>$LOGFILE
   
   if [ -n "$ACTIONGUID" ]
   then
        # Inform the API that we had a problem with this id:
        echo Send $PREINGEST_WEBAPI/api/status/failed/$ACTIONGUID
        json="{ \"message\": \"$message\" }"
        curl -s -S -X POST -H "Content-Type: application/json" --data "$json" "$PREINGEST_WEBAPI/api/status/failed/$ACTIONGUID" 
   fi
   
   exit 1
}

trap "moanAndDie" err

touch "$LOGFILE"

WHEREAMI=$(dirname $(realpath $0))
if [ -z "$JAVA_HOME" ]
then
   export JAVA_HOME=/usr/local/openjdk-8 # Hogere versie dan Java 8 niet ondersteund door SIP Creator
fi

if [ -z "$PREINGEST_WEBAPI" ]
then
    moanAndDie "$0: environment variable PREINGEST_WEBAPI is niet gedefinieerd".
fi

function doIt {
    # Obtain an actionGuid:
    # Sample result from /api/status/new: {"processId":"0b046a2d-26d7-406c-b832-d28b5d136114","folderSessionId":"0ee4629b-3394-6986-b859-430c0256ecd1","name":"SipCreatorHandler","description":"Create a preservica SIP file","creation":"2021-01-06T13:51:09.047071+00:00","resultFiles":"myfile.zip","status":null}
    # grep filters out the part "processId":"0b046a2d-26d7-406c-b832-d28b5d136114"
    # sed removes everything but the action guid (processId), even without quotes: 0b046a2d-26d7-406c-b832-d28b5d136114
    echo Retrieve actionguid using $PREINGEST_WEBAPI/api/status/new/$GUID
    ACTIONGUID=$(curl -s -S -X POST -H "Content-Type: application/json" --data '{ "name": "SipCreatorHandler", "description": "Create a preservica SIP file", "result": "myfile.zip" }' "$PREINGEST_WEBAPI/api/status/new/$GUID" | \
                 grep -o '"processId":"[^\"]*"' | \
                 sed -e 's/^.*: *"\([^\"]*\)"$/\1/')

    # Inform the API that we started the action with this id:
    echo Send $PREINGEST_WEBAPI/api/status/start/$ACTIONGUID
    curl -s -S -X POST -H "Content-Type: application/json" --data '{}' "$PREINGEST_WEBAPI/api/status/start/$ACTIONGUID"
    
    # Switches conform https://noordhollandsarchief.sharepoint.com/:x:/r/sites/ImplementatiePreserveringsvoorziening/Gedeelde%20documenten/General/2.%20Technische%20documentatie/Pre-ingest/CMD%20SIP%20creator%20settings.xlsx?d=w9e29f946e0624d7db2d1efeca5f3525b&csf=1&web=1&e=Z7CTt7
    # Let op: -excludedFileNames kan nuttig zijn, maar ondersteunt geen wildcards.
    
    # N.B. -ignoreparent is verwijderd omdat de klacht komt dat "Files are not allowed in the parent folder when not including the parent folder as a DU and not creating a DU per file"
    
    if [ -z "$PRESERVICA_REFERENCE" ]
    then
        "$WHEREAMI/createsip" \
            -input "$INPUTFOLDER" \
            -status NEW \
            -securitytag open \
            -sha512 \
            -export \
            -output "$OUTPUTFOLDER"
    else
        "$WHEREAMI/createsip" \
            -input "$INPUTFOLDER" \
            -status SAME \
            -colref "$PRESERVICA_REFERENCE" \
            -securitytag open \
            -sha512 \
            -export \
            -output "$OUTPUTFOLDER"
    fi
    
    cd "$OUTPUTFOLDER"
    zip -r "$OUTPUTFOLDER/$OUTPUTFOLDER_BASE.zip" !(*.log)
    
    rm -Rf !(*.zip|*.log)
    # Inform the API that we completed the action with this id:
    echo Send $PREINGEST_WEBAPI/api/status/completed/$ACTIONGUID
    curl -s -S -X POST -H "Content-Type: application/json" --data '{}' "$PREINGEST_WEBAPI/api/status/completed/$ACTIONGUID"    
}

cd "$INPUTFOLDER"

# Start the main function and pass its pid - $! - to disown sothat it will continue after this main script has exited..
# Note: disown is much like nohup, except that nohup requires the command to be a file and disown doesn't (there  are other differences, though):

doIt $1 $2 >"$LOGFILE" 2>&1 &
disown $!
