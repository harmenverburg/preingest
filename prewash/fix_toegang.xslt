<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xpath-default-namespace="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">

    <xsl:import href="_prewash-identity-transform.xslt"/>

    <!--
      Map old-style codes such as `Openbaar` to 2021 codes such as `publiek`, `publiek_metadata` and `intern`.
    -->
    <xsl:template match="aggregatie/openbaarheid/omschrijvingBeperkingen">
        <omschrijvingBeperkingen>
            <xsl:choose>
                <xsl:when test=". eq 'Openbaar'">publiek</xsl:when>
                <xsl:otherwise>{.}</xsl:otherwise>
            </xsl:choose>
        </omschrijvingBeperkingen>
    </xsl:template>

</xsl:stylesheet>
