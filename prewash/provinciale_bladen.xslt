<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xpath-default-namespace="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">

    <!--
      This stylesheet fixes errors in the sidecar metadata of the Provinciale Bladen of the Provincie Noord-Holland.
    -->

    <!-- In this case, this is actually also imported by all common fixes below -->
    <xsl:import href="_prewash-identity-transform.xslt"/>

    <!-- Common fixes; note that this may require URL percent-encoding for file names, such as `%20` for spaces -->
    <xsl:import href="fix_algoritme.xslt"/>
    <xsl:import href="fix_maximale_lengte_naam.xslt"/>
    <xsl:import href="fix_omvang.xslt"/>
    <xsl:import href="fix_toegang.xslt"/>

    <!-- On the archive level, the name may have a spelling error (missing dash) -->
    <xsl:template match="aggregatie/naam[../aggregatieniveau eq 'Archief' and . eq 'Provincie Noord Holland']/text()">
        <xsl:text>Provincie Noord-Holland</xsl:text>
    </xsl:template>
</xsl:stylesheet>
