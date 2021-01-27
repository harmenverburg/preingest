<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:topx="http://www.nationaalarchief.nl/ToPX/v2.3"
    xmlns:xip="http://www.tessella.com/XIP/v4"
    xmlns:saxon="http://saxon.sf.net/"
    xmlns="http://www.tessella.com/XIP/v4"
    default-mode="topx2xip"
    expand-text="yes"
    version="3.0">
    
    <xsl:mode on-no-match="shallow-copy"/>
    
    <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute('data-uri-prefix')"/>
    
    <!-- TODO variabele zorgdrager-geautoriseerde-naam kan misschien ook opgehaald worden uit de metadata op aggregatieniveau Archief. Nu o.b.v. request-parameter zorgdrager=... -->
    <xsl:variable name="zorgdrager-geautoriseerde-naam" as="xs:string?" select="string(/*/req:parameters/req:parameter[@name eq 'zorgdrager']/req:value)"/>  
    
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
    </xsl:template>
    
    <xsl:template name="create-xip-aggregatie-archief">
        <xsl:param name="topxDoc" as="document-node()" required="yes"/>
        
        <xsl:variable name="identificatiekenmerk" as="element(topx:identificatiekenmerk)" select="$topxDoc/*/topx:aggregatie/topx:identificatiekenmerk"/>
        <xsl:variable name="naam" as="element(topx:naam)" select="$topxDoc/*/topx:aggregatie/topx:naam"/>
        <xsl:variable name="omschrijvingBeperkingen" as="element(topx:omschrijvingBeperkingen)" select="$topxDoc/*/topx:aggregatie/topx:openbaarheid/topx:omschrijvingBeperkingen"/>
        
        <Collection status="new">
            <CollectionCode><xsl:apply-templates select="$identificatiekenmerk"/></CollectionCode>
            <Title><xsl:apply-templates select="$naam"/></Title>
            <SecurityTag><xsl:apply-templates select="$omschrijvingBeperkingen"/></SecurityTag>
            <Metadata><xsl:copy-of select="$topxDoc"/></Metadata>
        </Collection>
    </xsl:template>
    
    <xsl:template name="create-xip-aggregatie-serie-dossier-record">
        <xsl:param name="topxDoc" as="document-node()" required="yes"/>
        
        <xsl:variable name="identificatiekenmerk" as="element(topx:identificatiekenmerk)" select="$topxDoc/*/topx:aggregatie/topx:identificatiekenmerk"/>
        <xsl:variable name="naam" as="element(topx:naam)" select="$topxDoc/*/topx:aggregatie/topx:naam"/>
        <xsl:variable name="omschrijvingBeperkingen" as="element(topx:omschrijvingBeperkingen)" select="$topxDoc/*/topx:aggregatie/topx:openbaarheid/topx:omschrijvingBeperkingen"/>        
        <xsl:variable name="DigitalSurrogate" as="xs:string" select="'false'"/>
        <xsl:variable name="omschrijving" as="element(topx:omschrijving)?" select="()"/> <!-- TODO -->
        
        <DeliverableUnit status="new">
            <DigitalSurrogate>{$DigitalSurrogate}</DigitalSurrogate>
            <CatalogueReference><xsl:apply-templates select="$identificatiekenmerk"/></CatalogueReference>
            <ScopeAndContent><xsl:apply-templates select="$omschrijving"/></ScopeAndContent>
            <Title><xsl:apply-templates select="$naam"/></Title>
            <SecurityTag><xsl:apply-templates select="$omschrijvingBeperkingen"/></SecurityTag>
            <Metadata><xsl:copy-of select="$topxDoc"/></Metadata>
        </DeliverableUnit>
    </xsl:template>
    
    <xsl:template name="create-xip-aggregatie-bestand">
        <xsl:param name="topxDoc" as="document-node()" required="yes"/>
        
        <xsl:variable name="identificatiekenmerk" as="element(topx:identificatiekenmerk)" select="$topxDoc/*/topx:bestand/topx:identificatiekenmerk"/>
        <xsl:variable name="naam" as="element(topx:naam)" select="$topxDoc/*/topx:bestand/topx:naam"/>
        <xsl:variable name="bestandsverwijzing" as="element(topx:bestandsverwijzing)?" select="()"/> <!-- TODO -->
        <xsl:variable name="algoritme" as="element(topx:algoritme)?" select="$topxDoc/*/topx:bestand/topx:formaat/topx:fysiekeIntegriteit/topx:algoritme"/>
        <xsl:variable name="algoritme-waarde" as="element(topx:waarde)?" select="$topxDoc/*/topx:bestand/topx:formaat/topx:fysiekeIntegriteit/topx:waarde"/>
        <!-- Tijdelijk fix vanwege foutieve levering als <omvang>123456 bytes</omvang>: neem alleen het getal over. -->
        <xsl:variable name="omvang" as="xs:string" select="$topxDoc/*/topx:bestand/topx:formaat/topx:omvang => string() => replace('^\D*(\d+)\D*$', '$1')"/>
        
        <File status="new">
            <!-- TODO <FileRef> is in meerdere XML-conteksten toegestaan. Is dit wel de juiste plaats? Ook onder Manifestation/ManifestationFile kan het bijvoorbeeld. -->
            <xsl:if test="exists($bestandsverwijzing)"><FileRef><xsl:apply-templates select="$bestandsverwijzing"/></FileRef></xsl:if>
            <Metadata><xsl:copy-of select="$topxDoc"/></Metadata>
            <FileSize><xsl:value-of select="$omvang"/></FileSize>
            <FixityInfo>
                <FixityAlgorithmRef><xsl:apply-templates select="$algoritme"/></FixityAlgorithmRef>
                <FixityValue><xsl:apply-templates select="$algoritme-waarde"/></FixityValue>
            </FixityInfo>
            <Title><xsl:apply-templates select="$naam"/></Title>
        </File>
    </xsl:template>

    <xsl:template match="topx:omschrijvingBeperkingen">
        <xsl:variable name="zorgdrager" as="xs:string" select="if ($zorgdrager-geautoriseerde-naam ne '') then $zorgdrager-geautoriseerde-naam else '*zorgdrager-ontbreekt*'"/>
        <xsl:choose>
            <xsl:when test="text() eq 'Openbaar'">open</xsl:when>
            <xsl:when test="text() eq 'Beperkt openbaar A'">{$zorgdrager}_Niet_openbaar_A</xsl:when>
            <xsl:when test="text() eq 'Beperkt openbaar B'">{$zorgdrager}_Niet_openbaar_B</xsl:when>
            <xsl:when test="text() eq 'Beperkt openbaar C'">{$zorgdrager}_Niet_openbaar_C</xsl:when>
            <xsl:when test="matches(text(), 'Beperkt openbaar .*')">{$zorgdrager}_Niet_openbaar_{replace(text(), 'Beperkt openbaar (.*)', '$1')}</xsl:when> <!-- TODO is deze interpretatie juist? naam mappen, bijv. Zorgdrager=Gemeente_Haarlem -->
            
            <!-- TODO wat bij onbekend? -->
            <xsl:otherwise>{.}</xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template match="topx:algoritme">
        <xsl:choose>
            <xsl:when test="text() eq 'MD5'">1</xsl:when>
            <xsl:when test="text() eq 'SHA1'">2</xsl:when>
            <xsl:when test="text() eq 'SHA256'">3</xsl:when>
            <xsl:when test="text() eq 'SHA512'">4</xsl:when>
            
            <!-- TODO wat bij onbekend? -->
            <xsl:otherwise>{.}</xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
</xsl:stylesheet>