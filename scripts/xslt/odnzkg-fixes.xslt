<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xpath-default-namespace="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns:urldecoder="java:java.net.URLDecoder"
    exclude-result-prefixes="#all"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3" version="3.0" expand-text="yes">

    <xsl:mode on-no-match="shallow-copy"/>
    
    <xsl:variable name="fileinfodoc" as="document-node()" select="doc(base-uri() || '.tmp.xml')"/>
    
    <xsl:function name="nha:format-filedate" as="xs:string">
        <xsl:param name="filedate-from-stat" as="xs:string"/>
        
        <xsl:value-of select="$filedate-from-stat => substring-before(' ') || 'T' || $filedate-from-stat => substring-after(' ') => translate(' ', '') => replace('(\d\d)$', ':$1')"/>
    </xsl:function>

    <xsl:template match="aggregatieniveau/text()">
        <xsl:choose>
            <xsl:when test=". eq 'Document'"><xsl:text>Bestand</xsl:text></xsl:when>
            <xsl:when test=". eq 'Zaakniveau'">
                <xsl:comment>TODO Wat is de overeenkomstige waarde van Zaakniveau? Record? Dossier? Serie?</xsl:comment>
                <xsl:text>Record</xsl:text>
            </xsl:when>
            <xsl:otherwise><xsl:copy/></xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template match="bestand/omschrijving | aggregatie/omschrijving">
        <classificatie>
            <code>TODO-code</code>
            <xsl:copy>
                <xsl:apply-templates select="@* | node()"/>
            </xsl:copy>
            <bron>TODO-bron</bron>
        </classificatie>
    </xsl:template>
    
    <xsl:template match="bestand/naam[not(following-sibling::omschrijving)]">
        <xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy>
        <classificatie>
            <code>TODO-code</code>
            <omschrijving>TODO-omschrijving</omschrijving>
            <bron>TODO-bron</bron>
        </classificatie>
    </xsl:template>
    
    <xsl:template match="externeIdentificatiekenmerken">
        <externIdentificatiekenmerk>
            <xsl:apply-templates select="@* | node()"/>
        </externIdentificatiekenmerk>
    </xsl:template>

    <xsl:template match="externeIdentificatiekenmerken/kenmerkSysteem">
        <nummerBinnenSysteem>
            <xsl:apply-templates select="@* | node()"/>
        </nummerBinnenSysteem>
    </xsl:template>

    <xsl:template match="taal/text()[. eq 'Nederlands']">
         <xsl:comment>Vermoedelijk, op basis van http://www.loc.gov/standards/iso639-2/php/code_list.php Keuze tussen dut (B, bibliographic) en nld (T, terminology) en </xsl:comment>
        <xsl:text>dut</xsl:text>
    </xsl:template>
    
    <xsl:template match="classificatie/datum">
        <datumOfPeriode>
            <xsl:copy>
                <xsl:apply-templates select="@* | node()"/>
            </xsl:copy>
        </datumOfPeriode>
    </xsl:template>
    
    <xsl:template match="datum/text()[not(matches(., '^\d\d\d\d-\d\d?-\d\d?$'))]">
        <xsl:comment>TODO Datum heeft onjuist formaat: "{.}", aangepast</xsl:comment>
        <xsl:text>2021-02-11</xsl:text>
    </xsl:template>
    
    <xsl:template match="inTijd/begin[not(*)] | inTijd/eind[not(*)]">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <datum><xsl:apply-templates select="node()"/></datum>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="Periode">
        <periode>
            <xsl:apply-templates select="@*"/>
            <begin><datum>{substring-before(., ' ')}</datum></begin>
            <eind><datum>{substring-after(., '- ')}</datum></eind>
        </periode>
    </xsl:template>
    
    <xsl:template match="relatie[not(relatieID)]/typeRelatie">
        <relatieID>TODO-relatieID</relatieID>
        <xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy>
    </xsl:template>
    
    <xsl:template match="vertrouwelijkheid/classificatieNiveau">
        <xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy>
        <xsl:comment>TODO Datum of periode uitzoeken</xsl:comment>
        <datumOfPeriode><datum>2021-02-11</datum></datumOfPeriode>
    </xsl:template>
    
    <xsl:template match="Openbaarheid">
        <openbaarheid>
            <xsl:apply-templates select="@* | node()"/>
            <xsl:comment>TODO Datum of periode uitzoeken</xsl:comment>
            <datumOfPeriode><datum>2021-02-11</datum></datumOfPeriode>
        </openbaarheid>        
    </xsl:template>
    
    <xsl:template match="aggregatie/context[not(following-sibling::openbaarheid)]">
        <openbaarheid>
            <omschrijvingBeperkingen><!--omschrijvingBeperkingen: details staan nog niet vast; aangepast aan verouderde schema-conventie-->Beperkt openbaar A</omschrijvingBeperkingen>
            <!--TODO Datum of periode uitzoeken-->
            <datumOfPeriode>
                <datum>2021-02-11</datum>
            </datumOfPeriode>
        </openbaarheid>
    </xsl:template>
    
    <xsl:template match="omschrijvingBeperkingen/text()[. = ('internet', 'besloten')]">
        <xsl:comment>omschrijvingBeperkingen: details staan nog niet vast; aangepast aan verouderde schema-conventie</xsl:comment>
        <xsl:text>Beperkt openbaar A</xsl:text>
    </xsl:template>
    
    <xsl:template match="identificatieKenmerk">
        <identificatiekenmerk>
            <xsl:apply-templates select="@* | node()"/>
        </identificatiekenmerk>
    </xsl:template>
    
    <xsl:template match="bestandsnaam">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:choose>
                <xsl:when test="naam"><xsl:comment>Oorsponkelijke naam vervangen (bepaald op basis van de naam van het metadatabestand), oorspronkelijk stond er "{naam}"</xsl:comment></xsl:when>
                <xsl:otherwise><xsl:comment>Veld naam ontbrak, aangevuld op basis van de naam van het metadatabestand</xsl:comment></xsl:otherwise>
            </xsl:choose>
            <naam>{base-uri() => replace('^(.*/)*([^/]+\.[^.]+)\.metadata$', '$2')=>urldecoder:decode("UTF-8")}</naam>
            <xsl:apply-templates select="* except naam"/>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="bestandsformaat"/> <!-- Pulled na omvang -->
    
    <xsl:template match="bestandsformaat" mode="pull">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:comment>TODO is hier geen mimetype vereist?</xsl:comment>
            <xsl:apply-templates select="node()"/>
        </xsl:copy>
    </xsl:template>
        
    <xsl:template match="formaat/omvang">
        <xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy>
        <xsl:apply-templates select="../bestandsformaat" mode="pull"/>
    </xsl:template>

    <xsl:template match="formaat/omvang/text()">
        <xsl:comment>TODO omvang moet geheel getal zijn, in bytes</xsl:comment>
        <xsl:text>{$fileinfodoc/*/*:filesize}</xsl:text>
    </xsl:template>
    
    <xsl:template match="creatieapplicatie/naam">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:comment>TODO wat zijn hier de conventies?</xsl:comment>
            <xsl:apply-templates select="node()"/>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="creatieapplicatie">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
        
        <fysiekeIntegriteit>
            <algoritme>SHA512</algoritme>
            <waarde><xsl:comment>TODO bereken SHA512-waarde</xsl:comment>{$fileinfodoc/*/*:sha512sum}</waarde>
            <datumEnTijd><xsl:comment>TODO datum en tijd van bestand</xsl:comment>{nha:format-filedate($fileinfodoc/*/*:filedate)}</datumEnTijd>
        </fysiekeIntegriteit>
    </xsl:template>
    
</xsl:stylesheet>
