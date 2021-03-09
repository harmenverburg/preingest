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
    
    <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
    
    <xsl:template match="/">
        <xsl:variable name="reluri" as="xs:string" select="substring-after(/*/req:path, '/')"/>
        <xsl:copy-of select="doc($data-uri-prefix || encode-for-uri($reluri))"/>
    </xsl:template>
</xsl:stylesheet>