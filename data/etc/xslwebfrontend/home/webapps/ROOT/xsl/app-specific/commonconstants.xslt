<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="preingest-scheme-host-port" as="xs:string" required="yes"/>
    <xsl:param name="preingest-api-basepath" as="xs:string" required="yes"/>
    
    <xsl:variable name="nha:preingest-uri-prefix" as="xs:string" select="$preingest-scheme-host-port || $preingest-api-basepath"/>    
    <xsl:variable name="nha:context-path-key" as="xs:string" select="'context-path'" static="yes"/>
    <xsl:variable name="nha:actions-uri-prefix-key" as="xs:string" select="'actions-uri-prefix'"/>
    <xsl:variable name="nha:data-uri-prefix-key" as="xs:string" select="'data-uri-prefix'" static="yes"/>
    <xsl:variable name="nha:preingestguid-session-key" as="xs:string" select="'preingest-sessionguid'" static="yes"/>
    <xsl:variable name="nha:checksumtype-field" as="xs:string" select="'checksumtype'" static="yes"/>
    <xsl:variable name="nha:checksumvalue-field" as="xs:string" select="'checksumvalue'" static="yes"/>
    <xsl:variable name="nha:selectedfile-field" as="xs:string" select="'selectedfile'" static="yes"/>
    <xsl:variable name="nha:check-button" as="xs:string" select="'check'" static="yes"/>
    <xsl:variable name="nha:uncompress-button" as="xs:string" select="'uncompress'" static="yes"/>
    <xsl:variable name="nha:refresh-value" as="xs:integer" select="5000" static="yes"/>
</xsl:stylesheet>