<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:file="http://expath.org/ns/file"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    default-mode="topx2xip-folder"
    version="3.0">
    
    <xsl:param name="result-extension" as="xs:string" required="yes"/>
    
    <xsl:include href="topx2xip.xslt"/>
    
    <xsl:template match="/" mode="topx2xip-folder">
        <xsl:try>
            <xsl:variable name="reluri" as="xs:string" select="encode-for-uri(replace(/*/req:path, '^/[^/]+/(.*)$', '$1'))"/>
            <xsl:variable name="metadatafiles" as="xs:string*" select="file:list($data-uri-prefix || $reluri, true(), '*.metadata')"/>
            
            <metadatafiles data-uri-prefix="{$data-uri-prefix}" reluri="{$reluri}">
                <xsl:for-each select="$metadatafiles">
                    <xsl:variable name="metadatafile" as="xs:string" select="$data-uri-prefix || $reluri || '/' || encode-for-uri(.)"/>
                    <xsl:variable name="resultfile" as="xs:string" select="$metadatafile || $result-extension"/>
                    <metadatafile original="{$metadatafile}" result="{$resultfile}"/>
                    <xsl:variable name="result" as="document-node()">
                        <xsl:document>
                            <xsl:call-template name="topx2xip">
                                <xsl:with-param name="absuri" select="$metadatafile"/>
                            </xsl:call-template>
                        </xsl:document>
                    </xsl:variable>
                    <xsl:sequence select="file:write($resultfile, $result)"/>
                </xsl:for-each>
            </metadatafiles>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
</xsl:stylesheet>
