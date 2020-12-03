<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="preingest-scheme-host-port" as="xs:string" required="yes"/>
    
    <xsl:variable name="nha:data-uri-prefix-key" as="xs:string" select="'data-uri-prefix'" static="yes"/>
    <xsl:variable name="nha:full-swagger-json-uri-key" as="xs:string" select="'full-swagger-json-uri'" static="yes"/>
    <xsl:variable name="nha:sessionguid-key" as="xs:string" select="'sessionguid'" static="yes"/>
    <xsl:variable name="nha:checksumtype-field" as="xs:string" select="'checksumtype'" static="yes"/>
    <xsl:variable name="nha:checksumvalue-field" as="xs:string" select="'checksumvalue'" static="yes"/>
    <xsl:variable name="nha:selectedfile-field" as="xs:string" select="'selectedfile'" static="yes"/>
    <xsl:variable name="nha:check-button" as="xs:string" select="'check'" static="yes"/>
    <xsl:variable name="nha:uncompress-button" as="xs:string" select="'uncompress'" static="yes"/>
    <xsl:variable name="nha:checksum-condition" as="xs:string" select="'checksum-condition'" static="yes"/>
    <xsl:variable name="nha:refresh-value" as="xs:integer" select="10" static="yes"/>
</xsl:stylesheet>