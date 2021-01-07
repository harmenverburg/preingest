<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:external="http://www.armatiek.com/xslweb/functions/exec"
    xmlns:file="http://expath.org/ns/file"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">
    
    <xsl:mode on-no-match="shallow-skip"/>
    
    <xsl:param name="sipcreator-shellscript" required="yes" as="xs:string"/>
    
    <xsl:template match="/">
        <xsl:variable name="guid" as="xs:string" select="replace(/*/req:path, '^/([^/]+)/.*$', '$1')"/>
        <xsl:variable name="archive-folder-raw" as="xs:string" select="replace(/*/req:path, '^/[^/]+/([^/]+).*$', '$1')"/>
        <xsl:variable name="preservica-reference" as="xs:string?" select="/*/req:parameters/req:parameter[@name eq 'preservica-reference']/req:value"/>

        <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute('data-uri-prefix')"/>
        <xsl:variable name="sipcreator-folder" as="xs:string" select="req:get-attribute('sipcreator-folder')"/>    
        <xsl:variable name="sipcreator-script" as="xs:string" select="$sipcreator-folder || '/' || $sipcreator-shellscript"/>
        <xsl:variable name="archive-folder" as="xs:string" select="encode-for-uri($archive-folder-raw)"/>
        <xsl:variable name="inputdir" as="xs:string" select="file:path-to-native($data-uri-prefix || $guid || '/' || $archive-folder)"/>
        <xsl:variable name="scriptargs" as="xs:string+" select="($inputdir, $guid, $preservica-reference)"/>
        
        <result>
            <exit-status><xsl:sequence select="external:exec-external( $sipcreator-script, $scriptargs, xs:long(-1), false(), (), false())"/></exit-status>
        </result>
    </xsl:template>
    
</xsl:stylesheet>
