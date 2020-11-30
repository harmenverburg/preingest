<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="archives-file-glob" as="xs:string" required="yes"/>
    
    <xsl:output method="xml" indent="yes"/>
    
    <xsl:include href="commoncode.xslt"/>
    
    <xsl:template name="generate-container-file-table-rows">
        <xsl:variable name="recursive" as="xs:boolean" select="false()"/>
        <xsl:variable name="archives-folder-path" as="xs:string" select="file:path-to-native($data-uri-prefix || $archives-folder)"/>
        
        <xsl:for-each select="file:list($archives-folder-path, $recursive, $archives-file-glob)">
            <xsl:variable name="full-archive-path" as="xs:string" select="$archives-folder-path || file:dir-separator() || ."/>
            <xsl:message select="$full-archive-path"></xsl:message>
            <tr>
                <td><input type="radio" name="selectedfile" title="Kies dit bestand voor verdere verwerking"/></td>
                <td>{.}</td>
                <td>{file:size($full-archive-path)}</td>
                <td>{format-dateTime(file:last-modified($full-archive-path), '[Y]-[M01]-[D01] [H01]:[m01]')}</td> 
            </tr>
        </xsl:for-each>
    </xsl:template>
    
    <xsl:template match="/req:request">
        <html>
            <head>
                <title>Archiefselectie</title>
                <link rel="stylesheet" type="text/css" href="{$context-path}/css/gui.css" />
            </head>
            <body>
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
                
                <h2>Informatie over het geselecteerde bestand</h2>
                
                <p>Kies het type checksum:<br/>
                    <select>
                        <option selected="selected">Type&#x2026;</option>
                        <option>MD5</option>
                        <option>SHA1</option>
                        <option>SHA256</option>
                        <option>SHA512</option>
                    </select>
                </p>
                <p>Checksumwaarde, zoals verstrekt door de zorgdrager:<br/>
                    <textarea cols="50" rows="3" class="xx-small" placeholder="Plak hier de checksum van de zorgdrager"></textarea>
                </p>
                <p><button>Check&#x2026;</button>&#160;<button>Uitpakken&#x2026;</button></p>
                
                <p>Voor testen: <a href="operations">naar de operations-pagina</a>.</p>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>