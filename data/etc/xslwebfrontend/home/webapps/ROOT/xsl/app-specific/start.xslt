<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns="http://www.w3.org/1999/xhtml"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="archives-file-glob" as="xs:string" required="yes"/>
    
    <xsl:output method="xml" indent="no"/>
    
    <xsl:include href="commonconstants.xslt"/>
    <xsl:include href="commoncode.xslt"/>
    
    <!-- Pass ?full to the url in order to get a full <html> page and not just a <div> -->
    <xsl:variable name="full-html" as="xs:boolean" select="exists(/*/req:parameters/req:parameter[@name eq 'full'])"/>
    
    <xsl:template match="/req:request">
        <xsl:choose>
            <xsl:when test="$full-html">
                <html>
                    <head>
                        <title>Archiefselectie</title>
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
        <div id="main-div" data-prefix="{$nha:actions-uri-prefix}">
            <p><img src="/img/logo.png" style="float: right; width: 10em"/></p>
            <h1>Archiefselectie</h1>
            
            <table>
                <thead>
                    <tr>
                        <th>Selectie</th>
                        <th>Bestandsnaam</th>
                        <th>Omvang</th>
                        <th>Datum</th>
                    </tr>
                </thead>
                <tbody>
                    <xsl:call-template name="generate-container-file-table-rows"/>
                </tbody>
            </table>
            
            <h2>Verwerkingsgegevens voor het geselecteerde bestand:</h2>
            
            <xsl:variable name="checksumtype" as="xs:string" select="session:get-attribute($nha:checksumtype-field)"/>
            <p>Kies het type checksum:<br/>
                <select name="{$nha:checksumtype-field}"  id="{$nha:checksumtype-field}">
                    <option value="">Type&#x2026;</option> <!-- Geen @value, toont dat de gebruiker keuze moet maken -->
                    <option value="MD5">MD5</option>
                    <option value="SHA1">SHA1</option>
                    <option value="SHA256">SHA256</option>
                    <option value="SHA512">SHA512</option>
                </select>
            </p>
            <p>Checksumwaarde, zoals verstrekt door de zorgdrager:<br/>
                <textarea name="{$nha:checksumvalue-field}" id="{$nha:checksumvalue-field}" cols="50" rows="3" class="xx-small" placeholder="Plak hier de checksum van de zorgdrager"></textarea>
            </p>
            <p>
                <button type="submit" name="{$nha:check-button}" 
                    onclick="doCheckButton(this, '{$nha:uncompress-button}', '{$nha:checksumtype-field}', '{$nha:checksumvalue-field}', '{$nha:selectedfile-field}', {$nha:refresh-value})">Check&#x2026;</button>&#160;
                <button disabled="disabled" type="submit" name="{$nha:uncompress-button}" id="{$nha:uncompress-button}"
                    onclick="doUncompressButton(this, '{$nha:selectedfile-field}', {$nha:refresh-value})">Uitpakken&#x2026;</button>
            </p>
            <p style="display: none" id="proceedmessage">Het tarbestand is uitgepakt, ga nu <a id="proceedlink" href="operations/{if ($full-html) then '?full' else ''}">naar de operations-pagina</a>.</p>
        </div>
    </xsl:template>

    <xsl:template name="generate-container-file-table-rows">
        <xsl:variable name="recursive" as="xs:boolean" select="false()"/>
        
        <xsl:for-each select="file:list($nha:archives-folder-path, $recursive, $archives-file-glob)">
            <xsl:variable name="relative-path" as="xs:string" select="."/>
            <xsl:variable name="full-archive-path" as="xs:string" select="$nha:archives-folder-path || file:dir-separator() || $relative-path"/>
            <tr>
                <td><input type="radio" name="{$nha:selectedfile-field}" title="Kies dit bestand voor verdere verwerking" value="{encode-for-uri($relative-path)}"/></td>
                <td>{.}</td>
                <td>{file:size($full-archive-path)}</td>
                <td>{format-dateTime(file:last-modified($full-archive-path), '[Y]-[M01]-[D01] [H01]:[m01]')}</td> 
            </tr>
        </xsl:for-each>
    </xsl:template>
    
</xsl:stylesheet>