#!/bin/sh

USAGE="$0 stylesheet input output [ name=value ]..."

WHEREAMI=$(dirname $(realpath $0))

if [ -z "$3" ]
then
    echo $USAGE >&2
    exit 1
fi

if [ -z "$JAVA_HOME" ]
then
    echo Definieer JAVA_HOME s.v.p. >&2
    exit 1
fi

SAXONJAR=$WHEREAMI/../jar/saxon/saxon-he-10.3.jar

XSL=$1
IN=$2
OUT=$3

shift 3 # sothat we can process parameters...
if [ "$OUT" = "-" ]
then
  $JAVA_HOME/bin/java -classpath "$SAXONJAR" -Xms64m -Xmx4096m net.sf.saxon.Transform "$IN" "$XSL" "$@"
else
  $JAVA_HOME/bin/java -classpath "$SAXONJAR" -Xms64m -Xmx1024m net.sf.saxon.Transform "$IN" "$XSL" "$@" >"$OUT"
fi


