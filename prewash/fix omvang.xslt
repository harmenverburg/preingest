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
    
    <xsl:import href="_prewash-identity-transform.xslt"/>

    <!--
      Remove leading and trailing non-digits in ToPX `<omvang>`, such as a trailing ` bytes`. Note that the same fix is
      silently applied in topx2xip.xslt as well, but running this pre-wash will suppress validation errors.

      NOTE: one may want to be more specific, like to only expect/remove `bytes` but not units like `kB` or `MB`.
    -->
    <xsl:template match="bestand/formaat/omvang">
        <omvang>
            <xsl:value-of select="replace(., '^\D*(\d+)\D*$', '$1')"/>
        </omvang>
    </xsl:template>

</xsl:stylesheet>
