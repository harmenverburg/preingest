<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:json="http://www.w3.org/2005/xpath-functions"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <!-- Function to make it easy to deal with JSON in an XML environment.
         JSON is converted to XML. If the value of the key attribute allows it,
         the key defines the element name (and a type attribute keeps the original JSON type).
         Otherwise, xml-to-json()-element names are used.
         All elements are in the "json" namespace (http://www.w3.org/2005/xpath-functions).
    -->
    
    <xsl:mode name="nha:json-xml" on-no-match="shallow-copy"/>
    
    <xsl:function name="nha:json-doc-as-xml" as="document-node()">
        <xsl:param name="url"/>
        <xsl:variable name="json-string" as="xs:string" select="unparsed-text($url)"/>
        <xsl:apply-templates select="nha:json-to-xml($json-string)" mode="nha:json-xml"/>
    </xsl:function>
    
    <xsl:function name="nha:json-to-xml" as="document-node()">
        <xsl:param name="json-string" as="xs:string"/>
        <xsl:apply-templates select="json-to-xml($json-string)" mode="nha:json-xml"/>
    </xsl:function>
    
    <xsl:template match="*[@key => matches('^[-_a-zA-Z0-9]+$')]" mode="nha:json-xml">
        <xsl:element name="json:{@key}">
            <xsl:attribute name="type" select="local-name()"/>
            <xsl:apply-templates select="node()" mode="nha:json-xml"/>
        </xsl:element>
    </xsl:template>
</xsl:stylesheet>
