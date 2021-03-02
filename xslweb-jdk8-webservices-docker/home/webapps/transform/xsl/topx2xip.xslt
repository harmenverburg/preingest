<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:topx="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns:saxon="http://saxon.sf.net/"
    xmlns="http://www.tessella.com/XIP/v4"
    exclude-result-prefixes="#all"
    default-mode="topx2xip"
    expand-text="yes"
    version="3.0">
    
    <xsl:mode on-no-match="shallow-copy"/>
    
    <!-- The maximum size of a Title in XIP (currently, we don't know the real maximum): -->
    <xsl:param name="max-length-of-title" as="xs:integer" select="255"/>
    
    <xsl:variable name="zorgdrager-geautoriseerde-naam" as="xs:string" select="string(/*/req:parameters/req:parameter[@name eq 'Owner']/req:value)"/>
    <xsl:variable name="collection-status" as="xs:string" select="lower-case(/*/req:parameters/req:parameter[@name eq 'CollectionStatus']/req:value)"/>
    <xsl:variable name="CollectionRef" as="xs:string" select="lower-case(/*/req:parameters/req:parameter[@name eq 'CollectionRef']/req:value)"/>
    
    <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute('data-uri-prefix')"/>
        
    <!-- Wrapper function for non-standard call to discard-document() -->
    <xsl:function name="nha:discard-document" as="document-node()">
        <xsl:param name="doc" as="document-node()"/>
        <!-- Functie saxon:discard-document() is niet beschikbaar in de Saxon Home-editie. Het kan enorm op geheugen besparen als er heel veel XML-files zijn.
             N.B. bij de home-editie geeft function-avaible toch true() terug, dus onderstaande werkt niet:
             
             <xsl:sequence select="if (function-available('saxon:discard-document')) then saxon:discard-document($doc) else $doc"/>
             
             Maar gelukkig heeft XSLWeb zijn eigen discard-document(), dus in de context daarvan kunnen we die gebruiken:
        -->
        <xsl:sequence select="util:discard-document($doc)" xmlns:util="http://www.armatiek.com/xslweb/functions/util"/>
    </xsl:function>
    
    <xsl:template match="/" mode="topx2xip">
        <xsl:try>
            <xsl:variable name="reluri" as="xs:string" select="replace(/*/req:path, '^/[^/]+/(.*)$', '$1')"/>
            <xsl:call-template name="topx2xip">
                <xsl:with-param name="absuri" select="$data-uri-prefix || encode-for-uri($reluri)"/>
            </xsl:call-template>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
    <xsl:template name="topx2xip">
        <xsl:param name="absuri" as="xs:string" required="yes"/>
        
        <xsl:variable name="topxDoc" as="document-node()" select="nha:discard-document(doc($absuri))"/>
        
        <!-- Check if this is a ToPX document; if not, we assume it has already been converted to XIP earlier. In the last case, we just copy the document. -->
        <xsl:variable name="isToPX" as="xs:boolean" select="exists($topxDoc/topx:ToPX)"/>
        
        <xsl:choose>
            <xsl:when test="not($isToPX)">
                <xsl:copy-of select="$topxDoc"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:variable name="aggregatieniveau" as="xs:string" select="$topxDoc/*/*[self::topx:aggregatie | self::topx:bestand]/topx:aggregatieniveau"/>
                
                <xsl:choose>
                    <xsl:when test="$aggregatieniveau eq 'Archief'">
                        <xsl:call-template name="create-xip-aggregatie-archief">
                            <xsl:with-param name="topxDoc" select="$topxDoc"/>
                        </xsl:call-template>
                    </xsl:when>
                    <xsl:when test="$aggregatieniveau eq 'Dossier'">
                        <xsl:call-template name="create-xip-aggregatie-serie-dossier-record">
                            <xsl:with-param name="topxDoc" select="$topxDoc"/>
                        </xsl:call-template>
                    </xsl:when>
                    <xsl:when test="$aggregatieniveau eq 'Serie'">
                        <!-- TODO wacht op voorbeeld -->
                        <xsl:call-template name="create-xip-aggregatie-serie-dossier-record">
                            <xsl:with-param name="topxDoc" select="$topxDoc"/>
                        </xsl:call-template>
                    </xsl:when>
                    <xsl:when test="$aggregatieniveau eq 'Record'">
                        <xsl:call-template name="create-xip-aggregatie-serie-dossier-record">
                            <xsl:with-param name="topxDoc" select="$topxDoc"/>
                        </xsl:call-template>
                    </xsl:when>
                    <xsl:when test="$aggregatieniveau eq 'Bestand'">
                        <xsl:call-template name="create-xip-aggregatie-bestand">
                            <xsl:with-param name="topxDoc" select="$topxDoc"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template name="create-xip-aggregatie-archief">
        <xsl:param name="topxDoc" as="document-node()" required="yes"/>
        
        <xsl:variable name="identificatiekenmerk" as="element(topx:identificatiekenmerk)" select="$topxDoc/*/topx:aggregatie/topx:identificatiekenmerk"/>
        <xsl:variable name="naam" as="element(topx:naam)" select="$topxDoc/*/topx:aggregatie/topx:naam"/>
        <xsl:variable name="omschrijvingBeperkingen" as="element(topx:omschrijvingBeperkingen)" select="$topxDoc/*/topx:aggregatie/topx:openbaarheid/topx:omschrijvingBeperkingen"/>
        
        <!-- Possible values for status attribute (according to XIP-V4.xsd): new, same, changed, deleted, restored. Of course, in our case, only new and same apply. -->
        <Collection status="{$collection-status}">
            <xsl:if test="$collection-status ne 'new'"><CollectionRef><xsl:value-of select="$CollectionRef"/></CollectionRef></xsl:if>
            <CollectionCode><xsl:apply-templates select="$identificatiekenmerk"/></CollectionCode>
            <Title><xsl:apply-templates select="$naam" mode="title"/></Title>
            <SecurityTag><xsl:apply-templates select="$omschrijvingBeperkingen"/></SecurityTag>
            <Metadata schemaURI="http://www.nationaalarchief.nl/ToPX/v2.3"><xsl:copy-of select="$topxDoc"/></Metadata>
        </Collection>
    </xsl:template>
    
    <xsl:template name="create-xip-aggregatie-serie-dossier-record">
        <xsl:param name="topxDoc" as="document-node()" required="yes"/>
        
        <xsl:variable name="identificatiekenmerk" as="element(topx:identificatiekenmerk)" select="$topxDoc/*/topx:aggregatie/topx:identificatiekenmerk"/>
        <xsl:variable name="naam" as="element(topx:naam)" select="$topxDoc/*/topx:aggregatie/topx:naam"/>
        <xsl:variable name="omschrijvingBeperkingen" as="element(topx:omschrijvingBeperkingen)" select="$topxDoc/*/topx:aggregatie/topx:openbaarheid/topx:omschrijvingBeperkingen"/>        
        <xsl:variable name="DigitalSurrogate" as="xs:string" select="'false'"/>
        <xsl:variable name="omschrijving" as="element(topx:omschrijving)?" select="$topxDoc/*/topx:aggregatie/topx:omschrijving"/>
        
        <DeliverableUnit status="new">
            <DigitalSurrogate>{$DigitalSurrogate}</DigitalSurrogate>
            <CatalogueReference><xsl:apply-templates select="$identificatiekenmerk"/></CatalogueReference>
            <ScopeAndContent><xsl:apply-templates select="$omschrijving"/></ScopeAndContent>
            <Title><xsl:apply-templates select="$naam" mode="title"/></Title>
            <SecurityTag><xsl:apply-templates select="$omschrijvingBeperkingen"/></SecurityTag>
            <Metadata schemaURI="http://www.nationaalarchief.nl/ToPX/v2.3"><xsl:copy-of select="$topxDoc"/></Metadata>
        </DeliverableUnit>
    </xsl:template>
    
    <xsl:template name="create-xip-aggregatie-bestand">
        <xsl:param name="topxDoc" as="document-node()" required="yes"/>
        
        <xsl:variable name="naam" as="element(topx:naam)" select="$topxDoc/*/topx:bestand/topx:naam"/>
        <xsl:variable name="algoritme" as="element(topx:algoritme)?" select="$topxDoc/*/topx:bestand/topx:formaat/topx:fysiekeIntegriteit/topx:algoritme"/>
        <xsl:variable name="algoritme-waarde" as="element(topx:waarde)?" select="$topxDoc/*/topx:bestand/topx:formaat/topx:fysiekeIntegriteit/topx:waarde"/>
        <!-- Tijdelijk fix vanwege foutieve levering als <omvang>123456 bytes</omvang>: neem alleen het getal over. -->
        <xsl:variable name="omvang" as="xs:string" select="$topxDoc/*/topx:bestand/topx:formaat/topx:omvang => string() => replace('^\D*(\d+)\D*$', '$1')"/>
        
        <File status="new">
            <Metadata schemaURI="http://www.nationaalarchief.nl/ToPX/v2.3"><xsl:copy-of select="$topxDoc"/></Metadata>
            <FileSize><xsl:value-of select="$omvang"/></FileSize>
            <FixityInfo>
                <FixityAlgorithmRef><xsl:apply-templates select="$algoritme"/></FixityAlgorithmRef>
                <FixityValue><xsl:apply-templates select="$algoritme-waarde"/></FixityValue>
            </FixityInfo>
            <Title><xsl:apply-templates select="$naam" mode="title"/></Title>
        </File>
    </xsl:template>

    <xsl:template match="topx:naam" mode="title">
        <xsl:variable name="text" select="." as="xs:string"/>
        <xsl:choose>
            <xsl:when test="string-length($text) le $max-length-of-title"><xsl:value-of select="$text"/></xsl:when>
            <xsl:otherwise>
                <!-- Shorten the title by taking the first and the last part and inserting " ... " in between. -->
                <xsl:variable name="ellipsisstring" as="xs:string" select="' ... '"/>
                <xsl:variable name="half-max-length-of-title" as="xs:integer" select="xs:integer($max-length-of-title div 2)"/>
                <xsl:variable name="part1-end-offst" as="xs:integer" select="$half-max-length-of-title - string-length($ellipsisstring) - 1"/>
                <xsl:variable name="part2-start-offset" as="xs:integer" select="string-length($text) - $half-max-length-of-title"/>
                
                <xsl:variable name="part1" as="xs:string" select="substring($text, 1, $part1-end-offst)"/>
                <xsl:variable name="part2" as="xs:string" select="substring($text, $part2-start-offset)"/>
                <xsl:comment>Title truncated; original title:&#10;{$text}</xsl:comment>
                <xsl:value-of select="$part1 || $ellipsisstring || $part2"/>                
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="topx:omschrijvingBeperkingen">
        <xsl:variable name="zorgdrager" as="xs:string" select="if ($zorgdrager-geautoriseerde-naam ne '') then $zorgdrager-geautoriseerde-naam else '*zorgdrager-ontbreekt*'"/>
        <xsl:choose>
            <xsl:when test="text() eq 'Openbaar'">open</xsl:when>
            <xsl:when test="text() eq 'Beperkt openbaar A'">{$zorgdrager}_Niet_Openbaar_A</xsl:when>
            <xsl:when test="text() eq 'Beperkt openbaar B'">{$zorgdrager}_Niet_Openbaar_B</xsl:when>
            <xsl:when test="text() eq 'Beperkt openbaar C'">{$zorgdrager}_Niet_Openbaar_C</xsl:when>
            <xsl:when test="matches(text(), 'Beperkt openbaar .*')">{$zorgdrager}_Niet_Openbaar_{replace(text(), 'Beperkt openbaar (.*)', '$1')}</xsl:when> <!-- TODO is deze interpretatie juist? naam mappen, bijv. Zorgdrager=Gemeente_Haarlem -->
            
            <!-- TODO wat bij onbekend? -->
            <xsl:otherwise>{.}</xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template match="topx:algoritme">
        <xsl:variable name="adjusted-text" as="xs:string" select="text() => upper-case() => translate('-', '')"/>
        <xsl:choose>
            <xsl:when test="$adjusted-text eq 'MD5'">1</xsl:when>
            <xsl:when test="$adjusted-text eq 'SHA1'">2</xsl:when>
            <xsl:when test="$adjusted-text eq 'SHA256'">3</xsl:when>
            <xsl:when test="$adjusted-text eq 'SHA512'">4</xsl:when>
            
            <!-- TODO wat bij onbekend? -->
            <xsl:otherwise>{.}</xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
</xsl:stylesheet>