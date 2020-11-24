<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    expand-text="yes"
    version="3.0">
    
    <xsl:import href="planetsReport.xsl"/>
    
    <xsl:output method="html" version="5"/>
    
    <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute('data-uri-prefix')"/>
    
    <xsl:variable name="reluri" as="xs:string" select="string(/*/req:parameters/req:parameter[@name eq 'reluri']/req:value)"/>
    
    <xsl:template match="/">
        <xsl:try>
            <!-- Bij een nieuwe versie van planetsReport.xsl niet vergeten mode="start" toe te voegen aan het template voor "/" -->
            <xsl:apply-templates select="doc($data-uri-prefix || encode-for-uri($reluri))" mode="start"/>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
</xsl:stylesheet>