<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    expand-text="yes"
    version="3.0">
    
    <xsl:import href="droidReport.xsl"/>
    
    <xsl:output method="html" version="5"/>
    
    <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
    
    <xsl:variable name="reluri" as="xs:string" select="replace(/*/req:path, '^/[^/]+/(.*)$', '$1')"/>
    
    <xsl:template match="/req:request">
        <xsl:try>
            <xsl:apply-templates select="doc($data-uri-prefix || encode-for-uri($reluri))/*"/>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
</xsl:stylesheet>