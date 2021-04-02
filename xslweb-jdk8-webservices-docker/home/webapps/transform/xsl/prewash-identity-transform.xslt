<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">
    
    <xsl:mode on-no-match="shallow-copy"/>
    
    <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
    
    <xsl:template match="/">
        <xsl:try>
            <xsl:variable name="reluri" as="xs:string" select="replace(/*/req:path, '^/[^/]+/(.*)$', '$1')"/>
            <xsl:call-template name="nha:prewash">
                <xsl:with-param name="absuri" select="$data-uri-prefix || encode-for-uri($reluri)"/>
            </xsl:call-template>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
    <xsl:template name="nha:prewash">
        <xsl:param name="absuri" as="xs:string" required="yes"/>
        
        <xsl:variable name="topxDoc" as="document-node()" select="doc($absuri)"/>
        
        <xsl:apply-templates select="$topxDoc/*"/>
    </xsl:template>
</xsl:stylesheet>
