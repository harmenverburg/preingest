<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response" 
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="status" as="xs:integer" select="400"/>
    <xsl:param name="message" as="xs:string" select="'Something went wrong'"/>
    <xsl:param name="error-code" as="xs:string" select="'unknown-error'"/>
    
    <xsl:mode on-no-match="shallow-copy"/>
    
    <xsl:template match="/">
        <resp:response status="{$status}">
            <resp:headers>                              
                <resp:header name="Content-Type">text/xml</resp:header> 
            </resp:headers>
            <resp:body>
                <message error-code="{$error-code}">{$message}</message>
            </resp:body>
        </resp:response>          
    </xsl:template>
</xsl:stylesheet>