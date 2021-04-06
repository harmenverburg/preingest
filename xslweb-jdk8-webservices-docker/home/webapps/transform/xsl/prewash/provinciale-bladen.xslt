<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:topx="http://www.nationaalarchief.nl/ToPX/v2.3"
    xpath-default-namespace="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3"
    exclude-result-prefixes="#all" 
    expand-text="yes"
    version="3.0">
    
    <!-- This stylesheet fixes an error in the top level file of the Provinciale Bladen files of the Provincie Noord-Holland.
         On the archive level, the name may have a spelling error, which is corrected  here.
    -->
    
    <xsl:import href="_prewash-identity-transform.xslt"/>
    
    <xsl:template match="aggregatie/naam[../aggregatieniveau eq 'Archief' and . eq 'Provincie Noord Holland']/text()">
        <xsl:text>Provincie Noord-Holland</xsl:text>
    </xsl:template>
</xsl:stylesheet>
