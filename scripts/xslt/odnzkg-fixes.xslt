<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:math="http://www.w3.org/2005/xpath-functions/math" exclude-result-prefixes="xs math"
    xpath-default-namespace="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3" version="3.0" expand-text="yes">

    <xsl:mode on-no-match="shallow-copy"/>

    <xsl:template match="aggregatieniveau/text()[. eq 'Document']">
        <xsl:text>Bestand</xsl:text>
    </xsl:template>

    <xsl:template match="omschrijving">
        <classificatie>
            <code>TODO-code</code>
            <xsl:copy>
                <xsl:apply-templates select="@* | node()"/>
            </xsl:copy>
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
    
    <xsl:template match="identificatieKenmerk">
        <identificatiekenmerk>
            <xsl:apply-templates select="@* | node()"/>
        </identificatiekenmerk>
    </xsl:template>
    
    <xsl:template match="bestandsnaam">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:comment>TODO bestandsnaam, uri-encoding?</xsl:comment>
            <naam>{base-uri() => replace('^(.*/)*([^/]+)\.[^.]+\.metadata$', '$2')}</naam>
            <xsl:apply-templates/>
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
        <xsl:comment>TODO omvang moet geheel getal zijn</xsl:comment>
        <xsl:text>{translate(., '.', '')}</xsl:text>
    </xsl:template>
    
    <xsl:template match="creatieapplicatie/naam">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:comment>TODO wat zijn hier de conventies?</xsl:comment>
            <xsl:apply-templates select="node()"/>
        </xsl:copy>
    </xsl:template>
    
</xsl:stylesheet>
