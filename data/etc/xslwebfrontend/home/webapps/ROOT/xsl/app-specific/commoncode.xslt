<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:variable name="context-path" select="/*/req:context-path || /*/req:webapp-path" as="xs:string"/>

    <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute($nha:data-uri-prefix-key)"/>
    <xsl:variable name="full-swagger-json-uri" as="xs:string" select="req:get-attribute($nha:full-swagger-json-uri-key)"/>
    
    <!-- Folder below the data folder (relative path) where zorgdrager-specific archive files will be placed. Currently empty. -->
    <xsl:variable name="archives-folder" as="xs:string" select="''"/>
    
    <xsl:variable name="archives-folder-path" as="xs:string" select="file:path-to-native($data-uri-prefix || $archives-folder)"/>

    <xsl:variable name="swagger.json" as="map(*)" select="json-doc($full-swagger-json-uri)"/>
    <xsl:variable name="paths" as="map(*)" select="$swagger.json?paths"/>
    
    <xsl:function name="nha:get-from-swagger-paths" as="item()*">
        <xsl:param name="keyprefix" as="xs:string"/>
        <xsl:param name="get-or-post-key" as="xs:string"/>
        <xsl:variable name="pathkey-map-key" as="xs:string" select="map:keys($paths)[starts-with(., $keyprefix)]"/>
        <xsl:variable name="pathkey-map" as="map(*)" select="map:get($paths, $pathkey-map-key)"/>
        <xsl:variable name="get-or-post-map" as="map(*)" select="if (exists(($pathkey-map?get))) then $pathkey-map?get else $pathkey-map?post"/>
        <xsl:sequence select="map:get($get-or-post-map, $get-or-post-key)"/>
    </xsl:function>
    
    <xsl:function name="nha:get-swagger-description" as="item()*">
        <xsl:param name="keyprefix" as="xs:string"/>
        <xsl:sequence select="nha:get-from-swagger-paths($keyprefix, 'description')"/>
    </xsl:function>
    
    <xsl:function name="nha:is-post" as="xs:boolean">
        <xsl:param name="request" as="element(req:request)"/>
        <xsl:sequence select="$request/req:method eq 'POST'"/>
    </xsl:function>
    
    <xsl:function name="nha:get-parameter-value" as="xs:string*">
        <xsl:param name="request" as="element(req:request)"/>
        <xsl:param name="param-name" as="xs:string"/>
        <xsl:sequence select="$request/req:parameters/req:parameter[@name eq $param-name]/req:value"/>
    </xsl:function>
    
    <xsl:function name="nha:decode-uri" as="xs:string">
        <!-- TODO deze functie vervangen door een generiekere die alle %HH kan vervangen, xslweb extension mechanism. -->
        <xsl:param name="encoded-uri" as="xs:string"/>
        <xsl:value-of select="$encoded-uri => replace('%20', ' ') => replace('\+', ' ')"/>
    </xsl:function>
    
    <xsl:function name="nha:get-jsonpath-for-selected-archive" as="xs:string">
        <xsl:param name="relative-path" as="xs:string?"/>
        <xsl:param name="preingest-sessionid" as="xs:string?"/>
        <xsl:variable name="full-archive-path" as="xs:string" select="$archives-folder-path || file:dir-separator() || $relative-path"/>
        <xsl:variable name="full-json-path" as="xs:string" select="$full-archive-path || '_' || $preingest-sessionid || '.json'"/>

        <xsl:sequence select="$full-json-path"/>
    </xsl:function>
    
    <!-- The parameter is a path with zero or more slashes. Each part of the path is passed to encode-for-uri() and the concatenated result (with slashes) is returned. -->
    <xsl:function name="nha:encode-path-for-uri" as="xs:string">
        <xsl:param name="path" as="xs:string"/>
        <xsl:value-of select="string-join(for $f in tokenize($path, '/') return encode-for-uri($f), '/')"/>
    </xsl:function>

    <xsl:function name="nha:jsonfile-for-selected-archive-exists" as="xs:boolean">
        <xsl:param name="relative-path" as="xs:string?"/>
        <xsl:param name="sessionid" as="xs:string?"/>

        <xsl:sequence select="file:exists(nha:get-jsonpath-for-selected-archive($relative-path, $sessionid))"/>
    </xsl:function>

    <xsl:function name="nha:checksum-in-json-file-matches" as="xs:boolean">
        <xsl:param name="relative-path" as="xs:string?"/>
        <xsl:param name="preingest-sessionid" as="xs:string?"/>
        <xsl:param name="required-checksum-type"/>
        <xsl:param name="required-checksum-value"/>
        
        <xsl:variable name="json-uri" as="xs:anyURI" select="file:path-to-uri(nha:get-jsonpath-for-selected-archive($relative-path, $preingest-sessionid))"/>
        <xsl:variable name="json" as="array(*)" select="json-doc($json-uri)"/>
        <xsl:variable name="message-from-json" as="xs:string" select="$json?1?message"/>
        <xsl:variable name="type-from-json" as="xs:string" select="$message-from-json => replace('^([^:]+):.*$', '$1') => normalize-space()"/>
        <xsl:variable name="value-from-json" as="xs:string" select="$message-from-json => replace('^[^:]+:(.*)$', '$1') => normalize-space()"/>
        
        <xsl:sequence select="$type-from-json eq $required-checksum-type and $value-from-json eq $required-checksum-value"/>
    </xsl:function>
</xsl:stylesheet>