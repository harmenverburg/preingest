<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:topx="http://www.nationaalarchief.nl/ToPX/v2.3"
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="metadatafile" as="xs:string" required="yes"/>
    
    <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
    
    <xsl:template match="/">
        <xsl:variable name="guid" as="xs:string" select="tokenize(/*/req:path, '/')[2]"/>
        <xsl:variable name="url" as="xs:string" select="$data-uri-prefix || encode-for-uri($guid) || '/' || encode-for-uri($metadatafile)"/>
        <xsl:choose>
            <xsl:when test="doc-available($url)"><xsl:copy-of select="doc($url)"/></xsl:when>
            <xsl:otherwise>
                <XIP xmlns="http://www.tessella.com/XIP/v4">
                    <nha:error-file-not-found>File not found: "{$url}".</nha:error-file-not-found>
                </XIP>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>