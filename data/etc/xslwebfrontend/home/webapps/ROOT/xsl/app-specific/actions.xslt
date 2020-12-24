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
    <xsl:include href="json-xml.xslt"/>
    
    <xsl:function name="nha:http-post-request-json" as="item()*">
        <xsl:param name="url" as="xs:string"/>
        <xsl:param name="queryparams" as="xs:string?"/>
        
        <xsl:variable name="httprequest" as="element(http:http-request)">
            <http:http-request method="POST" href="{$url}">
                <!-- We want nothing else back but json -->
                <http:header name="Accept" value="application/json"/>
                <http:body media-type="application/x-www-form-urlencoded">dummy=dummy{if (string($queryparams) ne '') then '&amp;' || $queryparams else ''}</http:body>
            </http:http-request>
        </xsl:variable>
        
        <xsl:variable name="httpresponse" select="http:send-request($httprequest)"/>

        <xsl:choose>
            <xsl:when test="$httpresponse[1]/@status eq '200'">
                <xsl:choose>
                    <xsl:when test="starts-with($httpresponse[1]/http:body/@media-type, 'application/json')">
                        <xsl:try>
                            <xsl:sequence select="parse-json(base64:decode(string($httpresponse[2])))" xmlns:base64="http://www.armatiek.com/xslweb/functions/base64"/>
                            <xsl:catch>
                                <xsl:message>parsing json failed, errorcode={$err:code}, omschrijving="{$err:description}", module="{$err:module}", regelnummer="{$err:line-number}"</xsl:message>
                            </xsl:catch>
                        </xsl:try>
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
    
    <xsl:function name="nha:get-status-result-names" as="xs:string*">
        <xsl:param name="action-id" as="xs:string"/>
        
        <xsl:choose>
            <xsl:when test="$action-id ne ''">
                <xsl:variable name="json-uri" as="xs:string" select="$nha:status-uri-prefix || '/result/' || encode-for-uri($action-id)"/>
                
                <xsl:variable name="json-xml" as="document-node()" select="nha:json-doc-as-xml($json-uri)"/>
                <!--<xsl:sequence select="file:write('/data/json.xml', $json-xml)"/>-->
                <xsl:variable name="names" as="xs:string*" select="$json-xml//json:name"/>
                
                <xsl:sequence select="$names"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:text>Not-yet-implemented</xsl:text>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:function>
    
    <xsl:template match="/">
        <xsl:variable name="action" as="xs:string" select="nha:get-parameter-value(/req:request, 'action')"/>
        
        <xsl:variable name="response" as="item()*">
            <xsl:choose>
                <xsl:when test="$action eq 'action-result'">
                    <xsl:variable name="action-id" as="xs:string" select="nha:get-parameter-value(/req:request, 'action-id')"/>
                    <xsl:variable name="session-id" as="xs:string" select="nha:get-parameter-value(/req:request, 'session-id')"/>
                    <xsl:variable name="jsonresultfile" as="xs:string" select="nha:get-parameter-value(/req:request, 'jsonresultfile')"/>
                    
                    <xsl:try>
                        <xsl:variable name="names" as="xs:string*" select="nha:get-status-result-names($action-id)"/>
                        
                        <json:map>
                            <xsl:choose>
                                <xsl:when test="'Not-yet-implemented' = $names"><json:string key="status">not-yet-implemented</json:string></xsl:when>
                                <xsl:when test="'Failed' = $names"><json:string key="status">failed</json:string></xsl:when>
                                <xsl:when test="'Completed' = $names">
                                    <!--
                                        Haal de json op aan de hand van session-id en jsonresultfile
                                        Geef de waarde van Code in de JSON terug in code.
                                        TODO niet in elke result-JSON wordt de key "Code" toegepast...
                                    -->
                                    <xsl:variable name="result-json-url" as="xs:string" select="$nha:output-uri-prefix || '/json/' || $session-id || '/' || $jsonresultfile"/>
                                    <xsl:variable name="result-json" as="item()" select="json-doc($result-json-url)"/>
                                    <xsl:variable name="code">
                                        <xsl:choose>
                                            <!-- Resultcode may be spelt like Code or code. The contruction ( )[1] solves this.
                                                 TODO Deal with this, maybe uniform way of result status reporting.
                                            -->
                                            <xsl:when test="$result-json instance of map(*)">{($result-json?Code, $result-json?code)[1]}</xsl:when>
                                            <xsl:when test="$result-json instance of array(*)">check-log</xsl:when>
                                            <xsl:otherwise>unknown</xsl:otherwise>
                                        </xsl:choose>
                                    </xsl:variable>
                                    <json:string key="code">{$code}</json:string>
                                    <json:string key="status">completed</json:string>
                                </xsl:when>
                                <!-- Null indicates: still busy, try again later -->
                                <xsl:otherwise><json:null key="status"/></xsl:otherwise>
                            </xsl:choose>
                        </json:map>
                        <xsl:catch>
                            <xsl:message>actions.xslt, action={$action}, loading json failed, errorcode={$err:code}, omschrijving="{$err:description}", module="{$err:module}", regelnummer="{$err:line-number}"</xsl:message>
                            <json:map>
                                <json:string key="status">error</json:string>
                                <json:string key="message">loading json failed, errorcode={$err:code}, omschrijving="{$err:description}", module="{$err:module}", regelnummer="{$err:line-number}"</json:string>
                            </json:map>
                        </xsl:catch>
                    </xsl:try>
                </xsl:when>
                <xsl:when test="$action eq 'checksum-action-result'">
                    <xsl:variable name="action-id" as="xs:string" select="nha:get-parameter-value(/req:request, 'action-id')"/>
                    <xsl:variable name="tarfilename" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>

                    <xsl:try>
                        <xsl:variable name="names" as="xs:string*" select="nha:get-status-result-names($action-id)"/>
                        
                        <json:map>
                            <xsl:choose>
                                <xsl:when test="'Failed' = $names"><json:string key="status">failed</json:string></xsl:when>
                                <xsl:when test="'Completed' = $names">
                                    <!--<xsl:sequence select="file:write('/data/json2.xml', nha:json-doc-as-xml($nha:output-uri-prefix || '/collections'))"/>-->
                                    <xsl:variable name="message" select="nha:json-doc-as-xml($nha:output-uri-prefix || '/collections')//json:map[json:name eq $tarfilename]/json:tarResultData/json:map/json:message"/>
                                    <json:string key="status">completed</json:string>
                                    
                                    <json:string key="checksumType">{replace($message, '^([^:]*):.*$', '$1') => normalize-space()}</json:string>
                                    <json:string key="checksumValue">{replace($message, '^[^:]*:(.*)$', '$1') => normalize-space()}</json:string>
                                </xsl:when>
                                <!-- Null indicates: still busy, try again later -->
                                <xsl:otherwise><json:null key="status"/></xsl:otherwise>
                            </xsl:choose>
                        </json:map>
                        <xsl:catch>
                            <json:map>
                                <json:string key="status">error</json:string>
                                <json:string key="message">loading json failed, errorcode={$err:code}, omschrijving="{$err:description}", module="{$err:module}", regelnummer="{$err:line-number}"</json:string>
                            </json:map>
                        </xsl:catch>
                    </xsl:try>
                </xsl:when>
                <xsl:when test="$action eq 'calculate'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="checksum-type" as="xs:string" select="nha:get-parameter-value(/req:request, 'checksum-type')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/calculate/' || $checksum-type || '/' || encode-for-uri($relative-path)"/>
                    <xsl:variable name="json-doc" select="json-doc($json-uri)"/>
                    <json:map>
                        <json:string key="sessionId">{$json-doc?sessionId}</json:string>
                        <json:string key="actionId">{$json-doc?actionId}</json:string>
                    </json:map>
                </xsl:when>
                <xsl:when test="$action eq 'unpack'">
                    <xsl:variable name="relative-path" as="xs:string" select="nha:get-parameter-value(/req:request, 'relative-path')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/unpack/' || encode-for-uri($relative-path)"/>
                    <xsl:variable name="json-doc" select="json-doc($json-uri)"/>
                    <json:map>
                        <json:string key="sessionId">{$json-doc?sessionId}</json:string>
                        <json:string key="actionId">{$json-doc?actionId}</json:string>
                    </json:map>
                </xsl:when>
                <xsl:when test="$action = ('virusscan', 'naming', 'sidecar', 'greenlist', 'encoding', 'validate', 'transform')">
                    <xsl:variable name="sessionid" as="xs:string" select="nha:get-parameter-value(/req:request, 'sessionid')"/>
                    <xsl:variable name="json-uri" as="xs:string" select="$nha:preingest-uri-prefix || '/' || $action || '/' || encode-for-uri($sessionid)"/>
                    <xsl:variable name="json-doc" select="nha:http-post-request-json($json-uri, ())"/>
                    <json:map>
                        <json:string key="sessionId">{$sessionid}</json:string>
                        <json:string key="actionId">{$json-doc?actionId}</json:string>
                    </json:map>
                </xsl:when>
                <xsl:when test="$action = ('profiling', 'exporting', 'reporting', 'updatebinary', 'sipcreator')">
                    <!-- TODO Wacht op voorbeelden van JSON-bestanden (en hun namen) voor deze acties. -->
                    <!-- TODO sipcreator heeft misschien ook preservica-id (optioneel) nodig, indien bij transform gebruikt. -->
                    <xsl:variable name="sessionid" as="xs:string" select="nha:get-parameter-value(/req:request, 'sessionid')"/>
                    <json:map>
                        <json:string key="sessionId">{$sessionid}</json:string>
                        <json:string key="actionId"/>
                    </json:map>
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
