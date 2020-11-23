<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:validation="http://www.armatiek.com/xslweb/validation"
    xmlns="http://www.w3.org/2005/xpath-functions"
    xmlns:svrl="http://purl.oclc.org/dsdl/svrl"
    expand-text="yes"
    exclude-result-prefixes="#all"
    version="3.0">
    
    <xsl:mode on-no-match="shallow-skip"/>
    
    <xsl:param name="validation-detail-uri" required="yes" as="xs:string"/>
    
    <xsl:template match="/">
        <xsl:apply-templates/>
    </xsl:template>
    
    <xsl:template match="nha:folder-validation">
        <map>
            <array key="files-with-errors"><xsl:apply-templates/></array>
        </map>
    </xsl:template>
    
    <xsl:template match="nha:file-with-errors">
        <map>
            <string key="reluri">{@reluri}</string>
            <number key="schema-errors">{@count-schema-errors}</number>
            <number key="schematron-errors">{@count-schematron-errors}</number>
            <string key="detail-uri">{$validation-detail-uri}?reluri={@reluri}&amp;format=json</string>
        </map>
    </xsl:template>
</xsl:stylesheet>
