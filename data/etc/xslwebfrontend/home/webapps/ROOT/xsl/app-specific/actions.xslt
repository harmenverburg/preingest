<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:json="http://www.w3.org/2005/xpath-functions"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:output method="text"/>

    <xsl:include href="commonconstants.xslt"/>
    <xsl:include href="commoncode.xslt"/>
    
    <xsl:template match="/">
        <xsl:variable name="sessionid" as="xs:string?" select="session:get-attribute($nha:sessionguid-key)"/>
        
        <!-- E.g., /actions/calculate/md5sum/myfile.xyz is split into (1) "", (2) "actions", (3) "calculate", (4) "md5sum" and (5) "myfile.xyz". Note the empty string and also note that slashes in the filename should be encoded. -->
        <xsl:variable name="request-parts" as="xs:string+" select="tokenize(/*/req:path-info, '/')"/>
        <!-- dit is niet goed: moet maar een keer gebeuren. -->
        <xsl:variable name="dummy" select="json-doc($preingest-scheme-host-port || $preingest-api-basepath || '/' || string-join(subsequence($request-parts, 3), '/'))"/>
        <xsl:sequence select="file:write-text('/dev/null', string($dummy?dummy))"/>
        <xsl:variable name="response" as="element(json:map)">
            <xsl:choose>
                <xsl:when test="$request-parts[3] eq 'calculate'">
                    <xsl:variable name="relative-path" as="xs:string" select="$request-parts[5]"/>
                    <xsl:message select="'json-uri=' || $preingest-scheme-host-port || $preingest-api-basepath || '/' || string-join(subsequence($request-parts, 3), '/')"></xsl:message>
                    <xsl:choose>
                        <xsl:when test="nha:jsonfile-for-selected-archive-exists($relative-path, $sessionid)">
                            <json:map>
                                <json:string key="sessionId">{json-doc($preingest-scheme-host-port || $preingest-api-basepath || '/' || string-join(subsequence($request-parts, 3), '/'))?sessionId}</json:string>
                            </json:map>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:message>jsonfile is er nog niet</xsl:message>
                            <json:map><json:string key="sessionId">ff wachten</json:string></json:map>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:when>
            </xsl:choose>
        </xsl:variable>

        <resp:response status="200">
            <resp:headers>
                <resp:header name="Content-Type">application/json</resp:header>
                <resp:header name="x-clacks-overhead">GNU Terry Pratchett</resp:header>
            </resp:headers>
            <resp:body>
                <xsl:sequence select="xml-to-json($response)"/>
            </resp:body>
        </resp:response>
    </xsl:template>
    
</xsl:stylesheet>
