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
                        <resp:header name="Content-Type">text/html</resp:header> 
                    </resp:headers>
                    <resp:body>
                        <html>
                            <head>
                                <title>Error</title>
                            </head>
                            <body>
                                <h1>Error</h1>
                                <table>
                                    <th>error-code</th><td>{nha:error/@code}</td>
                                    <th>module</th><td>{nha:error/@module}</td>
                                    <th>line-number</th><td>{nha:error/@line-number}</td>
                                    <th>description</th><td>{nha:error/@description}</td>
                                </table>
                            </body>
                        </html>
                    </resp:body>
                </resp:response>
            </xsl:when>
            <xsl:otherwise>
                <resp:response status="200">
                    <resp:headers>                              
                        <resp:header name="Content-Type">text/html</resp:header> 
                    </resp:headers>
                    <resp:body>
                        <xsl:copy-of select="."/>
                    </resp:body>
                </resp:response>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>