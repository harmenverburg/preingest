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
    
    <xsl:template match="/req:request">
        <html>
            <head>
                <title>Archiefselectie</title>
                <xsl:if test="nha:is-post(.)"><meta http-equiv="refresh" content="{$nha:refresh-value}" /></xsl:if>
                <link rel="stylesheet" type="text/css" href="{$context-path}/css/gui.css" />
            </head>
            <body>
                <h1>Archiefselectie</h1>
                <xsl:if test="nha:is-post(.)">
                    <xsl:call-template name="do-post"/>
                </xsl:if>
                <form method="POST">
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
                        <select name="{$nha:checksumtype-field}">
                            <option>Type&#x2026;</option> <!-- Geen @value, toont dat de gebruiker keuze moet maken -->
                            <option value="MD5">MD5</option>
                            <option value="SHA1">SHA1</option>
                            <option value="SHA256">SHA256</option>
                            <option value="SHA512">SHA512</option>
                        </select>
                    </p>
                    <p>Checksumwaarde, zoals verstrekt door de zorgdrager:<br/>
                        <textarea name="{$nha:checksumvalue-field}" cols="50" rows="3" class="xx-small" placeholder="Plak hier de checksum van de zorgdrager"></textarea>
                    </p>
                    <p><button type="submit" name="{$nha:check-button}">Check&#x2026;</button>&#160;<button data-enablecondition="{$nha:checksum-condition}" type="submit" name="{$nha:uncompress-button}">Uitpakken&#x2026;</button></p>
                </form>
                
                <p>Voor testen: <a href="operations">naar de operations-pagina</a>.</p>
            </body>
        </html>
    </xsl:template>

    <xsl:template name="generate-container-file-table-rows">
        <xsl:variable name="recursive" as="xs:boolean" select="false()"/>
        
        <xsl:for-each select="file:list($archives-folder-path, $recursive, $archives-file-glob)">
            <xsl:variable name="relative-path" as="xs:string" select="."/>
            <xsl:variable name="full-archive-path" as="xs:string" select="$archives-folder-path || file:dir-separator() || $relative-path"/>
            <tr>
                <td><input type="radio" name="{$nha:selectedfile-field}" title="Kies dit bestand voor verdere verwerking" value="{encode-for-uri($relative-path)}"/></td>
                <td>{.}</td>
                <td>{file:size($full-archive-path)}</td>
                <td>{format-dateTime(file:last-modified($full-archive-path), '[Y]-[M01]-[D01] [H01]:[m01]')}</td> 
            </tr>
        </xsl:for-each>
    </xsl:template>
    
    <xsl:function name="nha:is-correct-check-parameters" as="xs:boolean">
        <xsl:param name="request" as="element(req:request)*"/>
        
        <xsl:sequence select="
            count(nha:get-parameter-value($request, $nha:selectedfile-field)) eq 1 and
            nha:get-parameter-value($request, $nha:checksumtype-field) = ('MD5', 'SHA1', 'SHA256', 'SHA512')"/>
    </xsl:function>
    
    <xsl:function name="nha:is-correct-uncompress-parameters" as="xs:boolean">
        <xsl:param name="request" as="element(req:request)*"/>
        
        <xsl:sequence select="nha:is-correct-check-parameters($request)"/>
    </xsl:function>
    
    <xsl:template name="do-post">
        <xsl:try>
            <xsl:variable name="basePath-from-swagger" as="xs:string" select="$swagger.json?basePath"/>
            <xsl:variable name="sessionid" as="xs:string?" select="session:get-attribute($nha:sessionguid-key)"/>
            <xsl:choose>
                <!-- Calculate checksum -->
                <xsl:when test="exists(nha:get-parameter-value(/req:request, $nha:check-button)) and nha:is-correct-check-parameters(/req:request)">
                    <xsl:variable name="url" as="xs:string"
                        select="$preingest-scheme-host-port || $basePath-from-swagger || '/calculate/' || nha:get-parameter-value(/req:request, $nha:checksumtype-field) || '/' || nha:get-parameter-value(/req:request, $nha:selectedfile-field)"/>
                    <xsl:variable name="response-json" as="map(*)" select="json-doc($url)"/>
                    <xsl:sequence select="session:set-attribute($nha:sessionguid-key, $response-json?sessionId)"/>
                </xsl:when>
                <!-- Uncompress tar -->
                <xsl:when test="exists(nha:get-parameter-value(/req:request, $nha:uncompress-button)) and nha:is-correct-uncompress-parameters(/req:request)">
                    <xsl:variable name="relative-path" as="xs:string?" select="nha:decode-uri(session:get-attribute($nha:selectedfile-field))"/>
                    <xsl:variable name="checksum-type" as="xs:string" select="session:get-attribute($nha:checksumtype-field)"/>
                    <xsl:variable name="checksum-value" as="xs:string" select="session:get-attribute($nha:checksumvalue-field)"/>
                    <xsl:if test="nha:jsonfile-for-selected-archive-exists($relative-path, $sessionid) and
                        nha:checksum-in-json-file-matches($relative-path, $sessionid, $checksum-type, $checksum-value)">
                        <xsl:variable name="url" as="xs:string"
                            select="$preingest-scheme-host-port || $basePath-from-swagger || '/unpack/' || nha:get-parameter-value(/req:request, $nha:selectedfile-field)"/>
                        <xsl:variable name="response-json" as="map(*)" select="json-doc($url)"/>
                        <xsl:sequence select="session:set-attribute($nha:sessionguid-key, $response-json?sessionId)"/>
                        <xsl:message>uncompress, sessionguid ="{session:get-attribute($nha:sessionguid-key)}"</xsl:message>
                    </xsl:if>
                </xsl:when>
                <xsl:otherwise>
                    <p class="input-error">De gegevens zijn niet correct ingevuld</p>
                </xsl:otherwise>
            </xsl:choose>
            <xsl:catch>
                <p class="application-error">Er is een fout opgetreden bij de verwerking. De fout is: {$err:code} - {$err:description}</p>
                <xsl:comment>err:code="{$err:code}" err:description="{$err:description}" err:module="{$err:module}" err:line-number="{$err:line-number}"</xsl:comment>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
</xsl:stylesheet>