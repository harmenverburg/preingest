<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns="http://www.nationaalarchief.nl/ToPX/v2.3"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">
    
    <!-- Mogelijke strings in de sequence voor DEBUG: 'schema', 'input' -->
    <!--<xsl:variable name="DEBUG" as="xs:string*" select="('schema', 'input')" static="yes"/>-->
    <xsl:variable name="DEBUG" as="xs:string*" select="()" static="yes"/>
    
    <xsl:variable name="is-bestand" as="xs:boolean" select="/*/is-directory eq 'false'"/>
    <xsl:variable name="filenaam" as="xs:string" select="/*/file"/>
    
    <xsl:output method="xml" encoding="UTF-8" indent="yes"/>
    
    <xsl:function name="nha:dirlevel2AggregatieNiveau" as="xs:string">
        <xsl:param name="dirlevel" as="xs:integer"/>
        <xsl:choose>
            <xsl:when test="$dirlevel eq 1">Archief</xsl:when>
            
            <!--<xsl:when test="$dirlevel eq 2">Record</xsl:when>-->
            
            <xsl:when test="$dirlevel eq 2">Dossier</xsl:when>
            <xsl:when test="$dirlevel eq 3">Record</xsl:when>
            
            <!--<xsl:when test="$dirlevel eq 2">Serie</xsl:when>
            <xsl:when test="$dirlevel eq 3">Dossier</xsl:when>
            <xsl:when test="$dirlevel eq 4">Record</xsl:when>-->
            <xsl:otherwise>Onbekend</xsl:otherwise>
        </xsl:choose>
    </xsl:function>
    
    <xsl:function name="nha:format-filedate" as="xs:string">
        <!-- Voorbeeld uitvoer van stat -\-format=%y:
             2017-08-31 10:02:14.000000000 +0200
             Vereist format:
             2017-08-31T10:02:14.000000000+02:00
        -->
        <xsl:param name="filedate-from-stat" as="xs:string"/>
        
        <xsl:value-of select="$filedate-from-stat => substring-before(' ') || 'T' || $filedate-from-stat => substring-after(' ') => translate(' ', '') => replace('(\d\d)$', ':$1')"/>
    </xsl:function>
    
    <xsl:attribute-set name="schema-attributes">
        <xsl:attribute name="xsi:schemaLocation" select="'http://www.nationaalarchief.nl/ToPX/v2.3 file:/home/pieter/noord-hollandsarchief/xsd/ToPX-2.3_2.xsd'" use-when="'schema' = $DEBUG"/>
    </xsl:attribute-set>
    
    <xsl:template match="/">
        <ToPX xsl:use-attribute-sets="schema-attributes">
            <xsl:if test="'input' = $DEBUG">
                <DEBUG-INPUT>
                    <xsl:copy-of select="/"/>
                </DEBUG-INPUT>
            </xsl:if>
            <xsl:element name="{if ($is-bestand) then 'bestand' else 'aggregatie'}">
                <identificatiekenmerk>{if ($is-bestand) then /*/directory || '/' || $filenaam else /*/directory}</identificatiekenmerk>
                <aggregatieniveau>{if ($is-bestand) then 'Bestand' else nha:dirlevel2AggregatieNiveau(xs:integer(/*/dirlevel))}</aggregatieniveau>
                <naam>{/*/file}</naam>
                <classificatie>
                    <code>code-{$filenaam}</code>
                    <omschrijving>classificatie-omschrijving-{$filenaam}</omschrijving>
                    <bron>bron-{$filenaam}</bron>
                </classificatie>
                <omschrijving>omschrijving-{$filenaam}</omschrijving>
                <dekking>
                    <inTijd>
                        <begin><jaar>2010</jaar></begin>
                        <eind><jaar>2020</jaar></eind>
                    </inTijd>
                    <geografischGebied>geografischGebied-{$filenaam}</geografischGebied>
                </dekking>
                <eventGeschiedenis>
                    <datumOfPeriode><datum>2020-12-09</datum></datumOfPeriode>
                    <type>eventGeschiedenis-type-{$filenaam}</type>
                    <verantwoordelijkeFunctionaris>eventGeschiedenis-verantwoordelijkeFunctionaris-{$filenaam}</verantwoordelijkeFunctionaris>
                </eventGeschiedenis>
                <relatie>
                    <relatieID>relatieID-{$filenaam}</relatieID>
                    <typeRelatie>typeRelatie-{$filenaam}</typeRelatie>
                </relatie>
                <context>
                    <actor>
                        <identificatiekenmerk>context-actor-identificatiekenmerk-{$filenaam}</identificatiekenmerk>
                        <geautoriseerdeNaam>context-actor-geautoriseerdeNaam-{$filenaam}</geautoriseerdeNaam>
                    </actor>
                </context>
                <gebruiksrechten>
                    <omschrijvingVoorwaarden>gebruiksrechten-omschrijvingVoorwaarden-{$filenaam}</omschrijvingVoorwaarden>
                    <datumOfPeriode><jaar>2020</jaar></datumOfPeriode>
                </gebruiksrechten>
                <vertrouwelijkheid>
                    <classificatieNiveau>Zeer geheim</classificatieNiveau>
                    <datumOfPeriode><jaar>2020</jaar></datumOfPeriode>
                </vertrouwelijkheid>
                <openbaarheid>
                    <omschrijvingBeperkingen>Openbaar</omschrijvingBeperkingen>
                    <datumOfPeriode><jaar>2020</jaar></datumOfPeriode>
                </openbaarheid>
                <xsl:choose>
                    <xsl:when test="$is-bestand">
                        <vorm>
                            <redactieGenre>redactieGenre-{$filenaam}</redactieGenre>
                        </vorm>
                        <formaat>
                            <identificatiekenmerk>identificatiekenmerk-{$filenaam}</identificatiekenmerk>
                            <bestandsnaam>
                                <naam>{$filenaam}</naam>
                            </bestandsnaam>
                            <omvang>{/*/filesize}</omvang>
                            <creatieapplicatie>
                                <naam>creatieapplicatie-naam-{$filenaam}</naam>
                            </creatieapplicatie>
                            <fysiekeIntegriteit>
                                <algoritme>SHA512</algoritme>
                                <waarde>{/*/sha512sum}</waarde>
                                <datumEnTijd>{nha:format-filedate(/*/filedate)}</datumEnTijd>
                            </fysiekeIntegriteit>
                        </formaat>
                    </xsl:when>
                    <xsl:otherwise>
                        <vorm>
                            <redactieGenre>redactieGenre-{$filenaam}</redactieGenre>
                        </vorm>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:element>
        </ToPX>
    </xsl:template>
</xsl:stylesheet>
