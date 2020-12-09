#!/bin/bash

if [ -z "$1" ]
then
    echo $0: pad naar folder vereist. Gestopt. >&2
    exit 1
fi

WHEREAMI=$(dirname $(realpath $0))

OUTEXT=metadata

genxml() {
    # TODO encoding is wrsch. afhankelijk van systeeminstellingen
    cat <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<root>
   <dirlevel>$dirlevel</dirlevel>
   <directory>$escapeddirname</directory>
   <file>$escapedfilename</file>
   <is-directory>$isdir</is-directory>
   <sha512sum>$sha512sum</sha512sum>
   <filesize>$filesize</filesize>
   <filedate>$filedate</filedate>
</root>
EOF
}

dodir() {
    # arg. 1: directory
    # arg. 2: level
    
    pushd "$1" >/dev/null
    echo Begonnen met de bewerking van directory $(pwd)
    
    for f in *
    do
        # lege folder, * expandeert niet
        if [ "$f" != '*' ]
        then
            if [ -d "$f" ]
            then
               # $f is een directory
               dodir "$f" $(($2 + 1))
            else
               # $f is een file
               extension=${f##*.}
               if [ $extension == $OUTEXT ]
               then
                   # Skip rommel van een eerdere run
                   echo Skip file $f
                   continue
               fi
               
               sha512sum=$(sha512sum -b "$f" | cut -d " " -f 1)
               filesize=$(stat --format="%s" "$f")
               filedate=$(stat --format=%y "$f")
               escapedfilename=$(echo $f | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g')
               escapeddirname=$(pwd | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g')
               isdir=false

               genxml | "$WHEREAMI/xslt.sh" "$WHEREAMI/../xslt/generate-sidecar.xslt" - "$f".$OUTEXT
            fi
        fi
    done
    
    # nu nog de metadata voor de folder zelf
    dirlevel=$2
    dirname=$(basename "$(pwd)")
    sha512sum=""
    filesize=""
    filedate=$(stat --format=%y "$(pwd)")
    escapedfilename=$(echo $dirname | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g')
    escapeddirname=$(pwd | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g')
    isdir=true
    
    genxml | "$WHEREAMI/xslt.sh" "$WHEREAMI/../xslt/generate-sidecar.xslt" - "$dirname".$OUTEXT
    
    echo Einde van de bewerking van directory $(pwd)
    popd >/dev/null
}

dodir "$1" 1
