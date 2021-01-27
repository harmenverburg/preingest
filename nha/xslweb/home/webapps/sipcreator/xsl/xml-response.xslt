<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:template match="/">
        <xsl:choose>
            <xsl:when test="/nha:error">
                <resp:response status="400">
                    <resp:headers>                              
                        <resp:header name="Content-Type">application/xml</resp:header>
                        <resp:header name="x-clacks-overhead">GNU Terry Pratchett</resp:header>
                    </resp:headers>
                    <resp:body>
                        <message error-code="{nha:error/@code}" module="{nha:error/@module}" line-number="{nha:error/@line-number}">{nha:error/@description}</message>
                    </resp:body>
                </resp:response>
            </xsl:when>
            <xsl:otherwise>
                <resp:response status="200">
                    <resp:headers>                              
                        <resp:header name="Content-Type">application/xml</resp:header>
                        <resp:header name="x-clacks-overhead">GNU Terry Pratchett</resp:header>
                    </resp:headers>
                    <resp:body>
                        <xsl:copy-of select="."/>
                    </resp:body>
                </resp:response>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>