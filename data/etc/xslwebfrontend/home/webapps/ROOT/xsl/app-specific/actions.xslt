<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:http="http://expath.org/ns/http-client"
    xmlns:json="http://www.w3.org/2005/xpath-functions"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:output method="text"/>

    <xsl:include href="commonconstants.xslt"/>
    <xsl:include href="commoncode.xslt"/>
    
    <xsl:function name="nha:http-post-request-json" as="element(json:map)">
        <xsl:param name="url" as="xs:string"/>
        <xsl:param name="queryparams" as="xs:string?"/>
        
        <xsl:variable name="httprequest" as="element(http:http-request)">
            <http:http-request method="POST" href="{$url}">
                <http:body media-type="application/x-www-form-urlencoded">dummy=dummy&amp;{$queryparams}</http:body>
            </http:http-request>
        </xsl:variable>
        
        <xsl:variable name="httpresponse" select="http:send-request($httprequest)"/>
        <!--<xsl:sequence select="file:write('/data/httpresponse.xml', $httpresponse[1])"/>-->
        <xsl:choose>
            <xsl:when test="$httpresponse[1]/@status eq '200'">
                <xsl:choose>
                    <xsl:when test="starts-with($httpresponse[1]/http:body/@media-type, 'application/json')">
                        <xsl:try>
                            <xsl:sequence select="parse-json($httpresponse[2])"/>
                            <xsl:catch>
                                <xsl:message>parsing json failed, errorcode={$err:code}, omschrijving="{$err:description}", module="{$err:module}", regelnummer="{$err:line-number}"</xsl:message>
                            </xsl:catch>
                        </xsl:try>
                        <!-- TODO Als de json niet parseert, is het wellicht base64binary. Hoe dan ook, we beschouwen dit tijdelijk als een goed resultaat. -->
                        <json:map>
                            <json:string key="code">OK</json:string>
                        </json:map>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:message>post-request response is geen json maar "{$httpresponse[1]/http:body/@media-type}"</xsl:message>
                        <json:map>
                            <json:string key="code">No-JSON</json:string>
                        </json:map>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            <xsl:otherwise>
                <xsl:message>post-request status = "{$httpresponse[1]/@status}"</xsl:message>
                <json:map>
                    <json:string key="code">status:{$httpresponse[1]/@status}</json:string>
                </json:map>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:function>
    
    <xsl:template match="/">
        <xsl:variable name="action" as="xs:string" select="nha:get-parameter-value(/req:request, 'action')"/>
        
        <xsl:variable name="response" as="element(json:map)">
            <xsl:choose>
                <xsl:when test="$action eq 'check-for-file'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="absolute-path" as="xs:string" select="$nha:archives-folder-path || file:dir-separator() || $relative-path"/>
                    
                    <xsl:choose>
                        <xsl:when test="file:exists($absolute-path)">
                            <xsl:message>Check: absolute-path={$absolute-path} -- BESTAAT</xsl:message>
                            <json:map>
                                <json:string key="code">OK</json:string>
                            </json:map>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:message>Check: absolute-path={$absolute-path} -- BESTAAT NIET</xsl:message>
                            <json:map>
                                <json:null key="code"/>
                            </json:map>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:when>
                <xsl:when test="$action eq 'check-for-file-with-ok'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="absolute-path" as="xs:string" select="$nha:archives-folder-path || file:dir-separator() || $relative-path"/>
                    
                    <xsl:variable name="absolute-uri" as="xs:anyURI" select="file:path-to-uri($nha:archives-folder-path || file:dir-separator() || nha:encode-path-for-uri($relative-path))"/>
                    <xsl:choose>
                        <xsl:when test="file:exists($absolute-path)">
                            <xsl:variable name="json" as="map(*)" select="json:json-doc($absolute-uri)"/>
                            <xsl:sequence select="session:set-attribute($nha:preingestguid-session-key, string($json?SessionId))"/>
                            <json:map>
                                <json:string key="sessionId">{$json?SessionId}</json:string>
                                <json:string key="code">{$json?Code}</json:string>
                            </json:map>
                        </xsl:when>
                        <xsl:otherwise>
                            <json:map>
                                <json:null key="sessionId"/>
                            </json:map>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:when>
                <xsl:when test="$action eq 'check-for-tar-json-file'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="preingest-session-id" as="xs:string" select="nha:get-parameter-value(/req:request, 'preingest-session-id')"/>
                    <xsl:choose>
                        <xsl:when test="nha:jsonfile-for-selected-archive-exists($relative-path, $preingest-session-id)">
                            <json:map>
                                <!-- The json file has an array with one element as its top level structure -->
                                <xsl:variable name="json" as="array(*)" select="json-doc(nha:get-jsonpath-for-selected-archive(encode-for-uri($relative-path), $preingest-session-id))"/>
                                <json:string key="sessionId">{$json?1?sessionId}</json:string>
                                <!-- Format of message, e.g. "message": "SHA256 : b46250879e806fe756e18b496d5679b1e3c56875dc012eebd5ae7e7d07f353cc" -->
                                <xsl:variable name="message" as="xs:string" select="$json?1?message"/>
                                <json:string key="checksumType">{replace($message, '^([^:]*):.*$', '$1') => normalize-space()}</json:string>
                                <json:string key="checksumValue">{replace($message, '^[^:]*:(.*)$', '$1') => normalize-space()}</json:string>
                            </json:map>
                        </xsl:when>
                        <xsl:otherwise>
                            <json:map>
                                <json:null key="sessionId"/>
                            </json:map>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:when>
                <xsl:when test="$action eq 'calculate'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="checksum-type" as="xs:string" select="nha:get-parameter-value(/req:request, 'checksum-type')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/calculate/' || $checksum-type || '/' || encode-for-uri($relative-path)"/>
                    <json:map>
                        <json:string key="sessionId">{json-doc($json-uri)?sessionId}</json:string>
                    </json:map>
                </xsl:when>
                <xsl:when test="$action eq 'unpack'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/unpack/' || encode-for-uri($relative-path)"/>
                    <json:map>
                        <json:string key="sessionId">{json-doc($json-uri)?sessionId}</json:string>
                    </json:map>
                </xsl:when>
                <xsl:when test="$action = ('virusscan', 'naming', 'sidecar', 'profiling', 'exporting', 'greenlist', 'encoding', 'validate', 'updatebinary', 'sipcreator')">
                    <!-- TODO sipcreator heeft misschien ook preservica-id (optioneel) nodig, indien bij transform gebruikt. -->
                    <xsl:message>action={$action}</xsl:message>
                    <xsl:variable name="sessionid" as="xs:string" select="nha:get-parameter-value(/req:request, 'sessionid')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/' || $action || '/' || encode-for-uri($sessionid)"/>
                    <xsl:sequence select="nha:http-post-request-json($json-uri, ())"/>
                </xsl:when>
                <xsl:when test="$action eq 'reporting'">
                    <!-- TODO dropdown voor pdf, xml, .. -->
                    <xsl:message>action={$action}</xsl:message>
                    <xsl:variable name="sessionid" as="xs:string" select="nha:get-parameter-value(/req:request, 'sessionid')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/' || $action || '/' || encode-for-uri($sessionid)"/>
                    <xsl:sequence select="nha:http-post-request-json($json-uri, ())"/>
                </xsl:when>
                <xsl:when test="$action eq 'transform'">
                    <!-- TODO optioneeel preservica-id, .. -->
                    <xsl:message>action={$action}</xsl:message>
                    <xsl:variable name="sessionid" as="xs:string" select="nha:get-parameter-value(/req:request, 'sessionid')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/' || $action || '/' || encode-for-uri($sessionid)"/>
                    <xsl:sequence select="nha:http-post-request-json($json-uri, ())"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:sequence select="()"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <resp:response status="{if (exists($response)) then 200 else 418}">
            <resp:headers>
                <resp:header name="Content-Type">application/json</resp:header>
                <resp:header name="x-clacks-overhead">GNU Terry Pratchett</resp:header>
            </resp:headers>
            <resp:body>
                <xsl:if test="exists($response)"><xsl:sequence select="xml-to-json($response)"/></xsl:if>
            </resp:body>
        </resp:response>
    </xsl:template>
    
</xsl:stylesheet>
