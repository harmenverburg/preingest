<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xpath-default-namespace="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3"
    exclude-result-prefixes="#all" 
    expand-text="yes"
    version="3.0">
    
    <xsl:import href="_prewash-identity-transform.xslt"/>

    <!--
      Replace `sha-512` with `SHA512` and all. Note that the same fix is silently applied in topx2xip.xslt as well. But
      running this pre-wash will suppress validation errors.
    -->
    <xsl:template match="bestand/formaat/fysiekeIntegriteit/algoritme">
        <algoritme>
            <xsl:value-of select="upper-case(translate(., '-', ''))"/>
        </algoritme>
    </xsl:template>

</xsl:stylesheet>
