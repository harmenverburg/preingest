<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:variable name="context-path" select="/*/req:context-path || /*/req:webapp-path" as="xs:string"/>

    <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute('data-uri-prefix')"/>
    
    <!-- Folder below the data folder (relative path) where zorgdrager-specific archive files will be placed. Currently empty. -->
    <xsl:variable name="archives-folder" as="xs:string" select="''"/>

    <xsl:variable name="swagger-json-uri" select="req:get-attribute('swagger-json-uri')"/>
    <xsl:variable name="swagger.json" as="map(*)" select="json-doc($swagger-json-uri)"/>
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
    
    
</xsl:stylesheet>