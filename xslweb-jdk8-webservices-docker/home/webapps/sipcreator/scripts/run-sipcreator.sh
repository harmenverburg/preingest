#!/bin/bash
if [ -z "$3" ]
then
    # Impossible to call moanAndDie: output directory has not yet been defined.
    echo "$0 requires 1. the path to the Preservica-supplied SIP Creator folder 2. an input folder with an archival structure, 3. a guid (corresponding with the session folder), 4. an optional Preservica reference". >&2
    exit 1
fi

SIPCREATOR_FOLDER=$1
INPUTFOLDER=$2
INPUTFOLDER_BASE=$(basename "$INPUTFOLDER")
OUTPUTFOLDER_BASE=sipresult
OUTPUTFOLDER=$INPUTFOLDER/../$OUTPUTFOLDER_BASE
GUID=$3
PRESERVICA_REFERENCE=$4

if [ -d "$OUTPUTFOLDER" ]
then
   rm -Rf "$OUTPUTFOLDER"
fi

mkdir "$OUTPUTFOLDER"

export LOGFILE=$OUTPUTFOLDER/../$OUTPUTFOLDER_BASE.log
export ACTIONGUID=""

shopt -s extglob

function notify() {
   starttime=$1
   endtime=$2
   success=$3 # "" is "Started", "0" is ok ("Completed"), anything else is not ok ("Failed")
   message=$4
   
   if [ -z "$success" ]
   then
      accepted=0
      rejected=0
      state=Started
   else
       if [ $success -eq 0 ]
       then
          accepted=1
          rejected=0
          state=Completed
       else
          accepted=0
          rejected=1
          state=Failed
       fi
   fi
   
   json="{ \"eventDateTime\": \"`timestamp`\", \
           \"sessionId\": \"$GUID\", \
           \"name\": \"SipCreatorHandler\", \
           \"state\": \"$state\", \
           \"message\": \"$message\", \
           \"hasSummary\": true, \
           \"processed\": 0, \
           \"accepted\": $accepted, \
           \"rejected\": $rejected, \
           \"start\": \"$starttime\", \
           \"end\": \"$endtime\" \
         }"
         
        # Send notification message to the API:
        echo Sending $PREINGEST_WEBAPI/api/Status/notify
        curl -s -S -X POST -H "Content-Type: application/json" --data "$json" "$PREINGEST_WEBAPI/api/Status/notify" 
        echo; echo "--------------- sent notify message for action guid $sessionid"
}

function moanAndDie() {
   echo "SIP creation terminated with an error"
    
   if [ -n "$1" ]
   then
       message=$1
   else
       message="An unknown error occurred"
   fi
   
   echo "$message"
   
   if [ -n "$ACTIONGUID" ]
   then
        # Inform the API that we had a problem with this id:
        echo Sending $PREINGEST_WEBAPI/api/Status/failed/$ACTIONGUID
        json="{ \"message\": \"$message\" }"
        curl -s -S -X POST -H "Content-Type: application/json" --data "$json" "$PREINGEST_WEBAPI/api/Status/failed/$ACTIONGUID" 
        echo; echo "--------------- sent FAILED message for action guid $ACTIONGUID"
        
        notify "$STARTTIME" "`timestamp`" 1 "$message"
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
    moanAndDie "$0: environment variable PREINGEST_WEBAPI is niet gedefinieerd". >>$LOGFILE
fi

function timestamp {
    date "+%Y-%m-%dT%H:%M:%S.%N%:z"
}

function doIt {
    # Obtain an actionGuid:
    # Sample result from /api/Status/new: {"processId":"0b046a2d-26d7-406c-b832-d28b5d136114","folderSessionId":"0ee4629b-3394-6986-b859-430c0256ecd1","name":"SipCreatorHandler","description":"Create a preservica SIP file","creation":"2021-01-06T13:51:09.047071+00:00","resultFiles":"myfile.zip","status":null}
    # grep filters out the part "processId":"0b046a2d-26d7-406c-b832-d28b5d136114"
    # sed removes everything but the action guid (processId), even without quotes: 0b046a2d-26d7-406c-b832-d28b5d136114
    echo Retrieve actionguid using $PREINGEST_WEBAPI/api/Status/new/$GUID
    OUTPUTZIP=$OUTPUTFOLDER/../$INPUTFOLDER_BASE.sip.zip
    json="{ \"name\": \"SipCreatorHandler\", \"description\": \"Create a preservica SIP file\", \"result\": \"$OUTPUTZIP\" }"
    ACTIONGUID=$(curl -s -S -X POST -H "Content-Type: application/json" --data "$json" "$PREINGEST_WEBAPI/api/Status/new/$GUID" | \
                 grep -o '"processId":"[^\"]*"' | \
                 sed -e 's/^.*: *"\([^\"]*\)"$/\1/')
    echo; echo "--------------- retrieved action guid $ACTIONGUID"

    # Inform the API that we started the action with this id:
    echo Send $PREINGEST_WEBAPI/api/Status/start/$ACTIONGUID
    curl -s -S -X POST -H "Content-Type: application/json" --data '{}' "$PREINGEST_WEBAPI/api/Status/start/$ACTIONGUID"
    echo; echo "--------------- sent START message for action guid $ACTIONGUID"
    
    # Also, do a notification:
    notify "$STARTTIME" "$STARTTIME" "" "SIPCreator was started"
    
    # Switches conform https://noordhollandsarchief.sharepoint.com/:x:/r/sites/ImplementatiePreserveringsvoorziening/Gedeelde%20documenten/General/2.%20Technische%20documentatie/Pre-ingest/CMD%20SIP%20creator%20settings.xlsx?d=w9e29f946e0624d7db2d1efeca5f3525b&csf=1&web=1&e=Z7CTt7
    # Let op: -excludedFileNames kan nuttig zijn, maar ondersteunt geen wildcards.
    
    # N.B. -ignoreparent is verwijderd omdat de klacht komt dat "Files are not allowed in the parent folder when not including the parent folder as a DU and not creating a DU per file"

    echo "Creating zip file $OUTPUTZIP"

    if [ -z "$PRESERVICA_REFERENCE" ]
    then
        "$WHEREAMI/nha-createsip" \
            "$SIPCREATOR_FOLDER" \
            -input "$INPUTFOLDER" \
            -status NEW \
            -securitytag open \
            -sha512 \
            -export \
            -output "$OUTPUTFOLDER"
    else
        "$WHEREAMI/nha-createsip" \
            "$SIPCREATOR_FOLDER" \
            -input "$INPUTFOLDER" \
            -status SAME \
            -colref "$PRESERVICA_REFERENCE" \
            -securitytag open \
            -sha512 \
            -export \
            -output "$OUTPUTFOLDER"
    fi

    cd "$OUTPUTFOLDER"

    if [ -f "$OUTPUTZIP" ]
    then
      rm -f "$OUTPUTZIP"
    fi

    zip -r -q "$OUTPUTZIP" !(*.log)

    ENDTIME=`timestamp`

    # Writing the status json to a tmp file because otherwise the escapes would make stuff rather unreadable...
    TMPJSON=$INPUTFOLDER/../_tmp.json
    if [ -s "$OUTPUTZIP" ]
    then
       OUTPUTZIP_CREATED=1
       
       cat >"$TMPJSON" <<EOF
{
    "result": "Success",
    "summary": "{\"processed\": 1,\"accepted\": 1,\"rejected\": 0,\"start\": \"$STARTTIME\",\"end\": \"$ENDTIME\"}"
}
EOF
    else
       OUTPUTZIP_CREATED=0
       
       cat >"$TMPJSON" <<EOF
{
    "result": "Error",
    "summary": "{\"processed\": 1,\"accepted\": 0,\"rejected\": 1,\"start\": \"$STARTTIME\",\"end\": \"$ENDTIME\"}"
}
EOF
    fi
    
    # Copy the metadata.xml to the toplevel folder of this session sothat we can later schema-validate it:
    SIPMETADATAFILE=$(find . -maxdepth 2 -name metadata.xml)
    if [ -n "$SIPMETADATAFILE" ]
    then
      cp "$SIPMETADATAFILE" "$INPUTFOLDER/.."
    fi

    cd ..
    rm -Rf "$OUTPUTFOLDER"
    
    #echo "About to send JSON for status/update:"; cat "$TMPJSON";  echo; echo "---------------"
    echo "Send $PREINGEST_WEBAPI/api/Status/update/$ACTIONGUID"
    curl -s -S -X PUT -H "Content-Type: application/json" --data @"$TMPJSON" "$PREINGEST_WEBAPI/api/Status/update/$ACTIONGUID"
    echo; echo "--------------- sent UPDATE message for action guid $ACTIONGUID"    
    rm "$TMPJSON"
    
    if [ $OUTPUTZIP_CREATED -eq 0 ]
    then
        moanAndDie "Missing or empty output zip"
    fi

    # Inform the API that we completed the action with this id:
    echo Send $PREINGEST_WEBAPI/api/Status/completed/$ACTIONGUID
    curl -s -S -X POST -H "Content-Type: application/json" --data '{}' "$PREINGEST_WEBAPI/api/Status/completed/$ACTIONGUID"
    echo; echo "--------------- sent COMPLETED message for action guid $ACTIONGUID"
    
    notify "$STARTTIME" "`timestamp`" 0 "SIPCreator has completed"
}

export STARTTIME=`timestamp`

cd "$INPUTFOLDER"

# Start the main function and pass its pid - $! - to disown sothat it will continue after this main script has exited..
# Note: disown is much like nohup, except that nohup requires the command to be a file and disown doesn't (there  are other differences, though):

doIt "$INPUTFOLDER" "$GUID" >"$LOGFILE" 2>&1 &
disown $!
