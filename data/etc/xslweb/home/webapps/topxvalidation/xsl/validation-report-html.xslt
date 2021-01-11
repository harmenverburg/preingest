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

    <xsl:template match="/">
        <html>
            <head>
                <title>Validation report</title>
                <style type="text/css" xsl:expand-text="no">
                    table { width: 60em; border-style: none; }
                    th { width: 10em; text-align: left; vertical-align: top; }
                    td { width: 50em; text-align: left; vertical-align: top; }
                    .message { font-weight: bold; }
                </style>
            </head>
            <body>
                <xsl:apply-templates/>
            </body>
        </html>
    </xsl:template>
    
    <xsl:template match="nha:report">
        <h1>Validation report</h1>
        <xsl:apply-templates/>
    </xsl:template>
    
    <xsl:template match="nha:schema-validation-report | nha:schematron-validation-report">
        <xsl:where-populated>
            <h2>{upper-case(substring(local-name(), 1, 1))}{replace(substring(local-name(), 2), '-', ' ')}</h2>
            <ol><xsl:apply-templates/></ol>
        </xsl:where-populated>
    </xsl:template>
    
    <xsl:template match="validation:validation-result">
        <li><xsl:apply-templates/></li>
    </xsl:template>
    
    <xsl:template match="validation:error">
        <table>
            <tr><th>line</th><td class="line">{@line}</td></tr>
            <tr><th>col</th><td class="col">{@col}</td></tr>
            <tr><th>message</th><td class="message">{.}</td></tr>
        </table>
    </xsl:template>
    
   <xsl:template match="svrl:failed-assert">
       <li>
           <table>
               <tr><th>test</th><td class="test">{@test}</td></tr>
               <tr><th>context</th><td  class="context">{preceding-sibling::svrl:fired-rule[1]/@context}</td></tr>
               <tr><th>location</th><td class="location">{@location}</td></tr>
               <tr><th>message</th><td class="message">{svrl:text}</td></tr>
           </table>
       </li>
    </xsl:template>

</xsl:stylesheet>
