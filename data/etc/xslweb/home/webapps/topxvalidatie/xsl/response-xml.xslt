<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:json="http://www.w3.org/2005/xpath-functions" exclude-result-prefixes="#all"
    expand-text="true" version="3.0">

    <xsl:param name="Content-Type" as="xs:string" required="yes"/>

    <xsl:output method="xml"/>

    <xsl:template match="/">
        <resp:response status="200">
            <resp:headers>
                <resp:header name="Content-Type">{$Content-Type}</resp:header>
            </resp:headers>
            <resp:body>
                <xsl:copy-of select="/"/>
            </resp:body>
        </resp:response>
    </xsl:template>
</xsl:stylesheet>
