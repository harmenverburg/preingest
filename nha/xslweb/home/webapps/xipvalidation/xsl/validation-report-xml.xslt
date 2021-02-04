<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet 
    xmlns="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"  
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    exclude-result-prefixes="#all"
    version="3.0">
    
    <xsl:param name="schema-validation-report" as="document-node()?"/>
    <xsl:param name="schematron-validation-report" as="document-node()?"/>
    
    <xsl:template match="/">
        <nha:report>
            <xsl:if test="$schema-validation-report">  
                <nha:schema-validation-report><xsl:copy-of select="$schema-validation-report"/></nha:schema-validation-report>
            </xsl:if>
            
            <xsl:if test="$schematron-validation-report">  
                <nha:schematron-validation-report><xsl:copy-of select="$schematron-validation-report"/></nha:schematron-validation-report>
            </xsl:if>
        </nha:report>
    </xsl:template>
    
</xsl:stylesheet>