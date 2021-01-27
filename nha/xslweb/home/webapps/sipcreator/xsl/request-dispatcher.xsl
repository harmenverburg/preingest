<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:pipeline="http://www.armatiek.com/xslweb/pipeline"
  xmlns:config="http://www.armatiek.com/xslweb/configuration"
  xmlns:log="http://www.armatiek.com/xslweb/functions/log"
  xmlns:req="http://www.armatiek.com/xslweb/request"
  xmlns:err="http://expath.org/ns/error"
  exclude-result-prefixes="#all" version="3.0" expand-text="yes">
  
  <xsl:param name="config:development-mode"/>
  <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
  <xsl:param name="data-uri-prefix-devmode" as="xs:string" required="yes"/>
  <xsl:param name="sipcreator-folder" as="xs:string" required="yes"/>
  <xsl:param name="sipcreator-folder-devmode" as="xs:string" required="yes"/>
  
  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    
    <!-- Dit request-attribuut voorkomt dat we deze logica telkens moeten herhalen: -->
    <xsl:sequence select="req:set-attribute('data-uri-prefix', if ($config:development-mode) then $data-uri-prefix-devmode else $data-uri-prefix)"/>
    <xsl:sequence select="req:set-attribute('sipcreator-folder', if ($config:development-mode) then $sipcreator-folder-devmode else $sipcreator-folder)"/>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="/req:request[count(tokenize(req:path, '/')) ge 3]">
    <!-- 3 parts, as the path starts with a /, the format is http:/host:port/guid/directory -->
    <xsl:sequence select="log:log('INFO', 'Dealing with request-path ' || /req:request/req:path)"/>
    
    <pipeline:pipeline>
      <pipeline:transformer name="sipcreator" xsl-path="sipcreator.xslt"/>
      <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
</xsl:stylesheet>
