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
    
    <!-- This stylesheet serves as a simple example of what a specific prewash stylesheet could look like.
         Essential is the include of prewash-identity-transform.xslt, the stylesheet that also serves as the
         default stylesheet when the request dispatcher does not find a specific one.
         
         Anything else is nothing more than defining normal XSLT processing in order to apply the prewash fixes.
    -->
    
    <xsl:import href="prewash-identity-transform.xslt"/>
    
    <!-- This rule changes the erroneous archive name "Provincie Noord Holland" into the correct name "Provincie Noord-Holland".
         It applies the rule only if the incorrect string is found, and only on the Archief aggregatieniveau.
    -->
    <xsl:template match="aggregatie/naam[../aggregatieniveau eq 'Archief' and . eq 'Provincie Noord Holland']">
        <naam>Provincie Noord-Holland</naam>
    </xsl:template>
</xsl:stylesheet>
