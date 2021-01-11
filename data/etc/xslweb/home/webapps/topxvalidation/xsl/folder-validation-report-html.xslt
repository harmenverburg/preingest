<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:validation="http://www.armatiek.com/xslweb/validation"
    xmlns:svrl="http://purl.oclc.org/dsdl/svrl"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">
    
    <xsl:mode on-no-match="shallow-skip"/>
    
    <xsl:param name="validation-detail-uri" required="yes" as="xs:string"/>
    
    <xsl:template match="/">
        <html>
            <head>
                <title>Validation report</title>
                <style type="text/css" xsl:expand-text="no">
                    table { width: 80em; border-style: none; }
                    .reluri { width: 60em; text-align: left; vertical-align: top; }
                    td.reluri { font-family: monospace; font-size: xx-small; }
                    .schema-errors, .schematron-errors { width: 10em; text-align: left; vertical-align: top; }
                </style>
            </head>
            <body>
                <xsl:apply-templates/>
            </body>
        </html>
    </xsl:template>
    
    <xsl:template match="nha:folder-validation">
        <h1>Validation report for folder {@data-uri-prefix || @reluri}</h1>
        <table>
            <thead>
                <tr>
                    <th class="reluri">relative uri</th>
                    <th class="schema-errors"># schema errors</th>
                    <th class="schematron-errors"># schematron errors</th>
                </tr>
            </thead>
            <tbody><xsl:apply-templates/></tbody>
        </table>
    </xsl:template>
    
    <xsl:template match="nha:file-with-errors">
        <tr>
            <td class="reluri"><a href="{$validation-detail-uri}/{@reluri}?format=html">{@reluri}</a></td>
            <td class="schema-errors">{@count-schema-errors}</td>
            <td class="schematron-errors">{@count-schematron-errors}</td>
        </tr>
    </xsl:template>
        
</xsl:stylesheet>
