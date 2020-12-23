<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    expand-text="yes"
    version="3.0">
    
    <xsl:output method="html" version="5"/>
    
    <xsl:include href="commonconstants.xslt"/>
    <xsl:include href="commoncode.xslt"/>
    
    <!-- Pass ?full to the url in order to get a full <html> page and not just a <div> -->
    <xsl:variable name="full-html" as="xs:boolean" select="exists(/*/req:parameters/req:parameter[@name eq 'full'])"/>
    
    <xsl:variable name="preingestguid" as="xs:string?" select="replace(/*/req:path, '^.*/([-a-z0-9]+)$', '$1')"/>
    
    <xsl:template match="/req:request">        
        <xsl:choose>
            <xsl:when test="$full-html">
                <html xmlns="http://www.w3.org/1999/xhtml">
                    <head>
                        <title>Archiefbewerkingen</title>
                        <link rel="stylesheet" type="text/css" href="{$nha:context-path}/css/gui.css" />
                        <script language="javascript" src="{$nha:context-path}/js/gui.js" type="text/javascript"></script>
                    </head>
                    <body>
                        <xsl:call-template name="body-content"/>
                    </body>
                </html>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="body-content"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template name="body-content">
        <div id="main-div" data-guid="{$preingestguid}" data-prefix="{$nha:actions-uri-prefix}">
            <p><img src="/img/logo.png" style="float: right; width: 10em"/></p>
            <xsl:choose>
                <xsl:when test="exists($preingestguid)">
                    <xsl:call-template name="normal-body"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:call-template name="error-body"/>
                </xsl:otherwise>
            </xsl:choose>
        </div>
    </xsl:template>
    
    <xsl:template name="error-body">
        <h1>Fout</h1>
        <p class="error">Er is geen waarde in de sessie aanwezig met de naam van de folder met de uitgepakte bestanden.</p>
    </xsl:template>
    
    <xsl:template name="normal-body">
        <h1>Archiefbewerkingen</h1>
        <h2>Uitgepakte bestanden controleren en bewerken<br/>
            De folder met uitgepakte bestanden is {$preingestguid}</h2>
        <table>
            <tbody>
                <tr>
                    <th>Scan uitvoeren voor het detecteren van virussen</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="ScanVirusValidationHandler.json" data-action="virusscan">Viruscontrole&#x2026;</button></td>
                </tr>
                <tr style="display: none;">
                    <td colspan="2">
                        <div class="report">
                            <p style="font-weight: bold">Deze tekst is standaard verborgen maar
                                wordt zichtbaar zodra de bijbehorende actie voltooid is.</p>
                            <p>Nemo enim ipsam voluptatem, quia voluptas sit, aspernatur aut odit
                                aut fugit, sed quia consequuntur magni dolores eos, qui ratione
                                voluptatem sequi nesciunt, neque porro quisquam est, qui dolorem
                                ipsum, quia dolor sit, amet, consectetur, adipisci velit, sed quia
                                non numquam eius modi tempora incidunt, ut labore et dolore magnam
                                aliquam quaerat voluptatem. ut enim ad minima veniam, quis nostrum
                                exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid
                                ex ea commodi consequatur?</p>
                        </div>
                    </td>
                    <td></td>
                </tr>
                <tr>
                    <th>Benamingen van mappen en bestanden controleren op ongewenste karakters en gereserveerde bestandsnamen</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="NamingValidationHandler.json" data-action="naming">Bestandsnamen controleren&#x2026;</button></td>
                </tr>
                <tr style="display: none">
                    <td colspan="2"><div class="report">
                        <p style="font-weight: bold">Deze tekst is ook standaard verborgen; meer
                            voorbeelden nemen we echter niet op.</p>
                        <p>Je zou ook kunnen overwegen om kleuren te gebruiken om te laten zien
                            of iets goed of fout gegaan is (rood/groen).</p>
                    </div></td>
                    <td></td>
                </tr>
                <tr>
                    <th>Mappen en bestanden controlen op sidecarstructuur</th>
                    <!-- TODO naast SidecarValidationHandler_Archief.json heb je ook SidecarValidationHandler_Dossier.json, etc. -->
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="SidecarValidationHandler_Archief.json" data-action="sidecar">Sidecarstructuur controleren&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Droid voorbereiden om een map (en onderliggende objecten) te scannen voor metagegevens</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="{$preingestguid}.droid" data-action="profiling">Droid voorbereiden&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Droid metagegevens exporteren naar een CSV-bestand; vereist eerst de actie 'Droid voorbereiden&#x2026;'</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="{$preingestguid}.droid.csv" data-action="exporting">CSV-bestand aanmaken&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Droid metagegevens exporteren naar een PDF- of een XML-bestand; vereist eerst de actie 'Droid voorbereiden&#x2026;' [TODO]</th>
                    <td>
                        <select>
                            <option value="">Maak een keuze</option>
                            <option value="pdf">PDF</option>
                            <option value="droid">Droid-XML</option>
                            <option value="planets">Planets-XML</option>
                        </select>
                        <br/>
                        <button disabled="disabled" onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="TODO.json" data-action="reporting">PDF- of XML-bestand aanmaken&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Bestanden controleren of deze in de 'greenlist' voorkomen. Vereist eerst de actie 'Droid metagegevens exporteren&#x2026;'</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="GreenListHandler.json" data-action="greenlist">Greenlistcontrole&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Metadata bestanden controleren op de encoding</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="EncodingHandler.json" data-action="encoding">Encodingcontrole&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Metadata valideren aan de hand van xml-schema (XSD) en schematron</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="MetadataValidationHandler.json" data-action="validate">Schema(tron)-controle&#x2026;</button></td>
                </tr>
                <tr style="display: none">
                    <td colspan="2"><div class="report">
                        <p style="font-weight: bold">De tekst zou een rapportage kunnen bevatten van alle schema- en schematron-errors, of een link naar een aparte pagina met die fouten.</p>
                    </div></td>
                    <td></td>
                </tr>
                <tr>
                    <th>Metadata bestanden transformeren naar XIP bestandsformaat</th>
                    <td>
                        <input type="text" size="40" placeholder="Optioneel: preservica-id voor toevoeging" value="" />
                        <br/>
                        <button onclick="doOperationsButton(this, {$nha:refresh-value})" data-waitforfile="TransformationHandler.json" data-action="transform">Omvormen naar XIP&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Binary bestand bijwerken met PRONOM, Greenlist en Encoding gegevens (indien deze data beschikbaar zijn) [TODO]</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" disabled="disabled" data-waitforfile="TODO.json" data-action="updatebinary">Binary bijwerken&#x2026;</button></td>
                </tr>
                <tr>
                    <th>Bestanden laten verwerken door Preservica SIP Creator [TODO]</th>
                    <td><button onclick="doOperationsButton(this, {$nha:refresh-value})" disabled="disabled" data-waitforfile="TODO.json" data-action="sipcreator">Sipcreator&#x2026;</button></td>
                </tr>
            </tbody>
        </table>
    </xsl:template>
</xsl:stylesheet>