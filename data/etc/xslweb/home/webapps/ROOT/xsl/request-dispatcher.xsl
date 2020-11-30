<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:pipeline="http://www.armatiek.com/xslweb/pipeline"
  xmlns:config="http://www.armatiek.com/xslweb/configuration"
  xmlns:req="http://www.armatiek.com/xslweb/request" xmlns:err="http://expath.org/ns/error"
  exclude-result-prefixes="#all" version="3.0" expand-text="yes">

  <xsl:param name="config:development-mode"/>
  <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
  <xsl:param name="data-uri-prefix-devmode" as="xs:string" required="yes"/>
  <xsl:param name="swagger-json-uri" as="xs:string" required="yes"/>

  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    
    <xsl:variable name="data-uri-prefix" as="xs:string" select="if ($config:development-mode) then $data-uri-prefix-devmode else $data-uri-prefix"/>

    <!-- These request attributes serve to prevent duplication of code: -->
    <xsl:sequence select="req:set-attribute('data-uri-prefix', $data-uri-prefix)"/>

    <xsl:sequence select="req:set-attribute('swagger-json-uri', $data-uri-prefix || $swagger-json-uri)"/>
      
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="/req:request[req:path eq '/' or req:path eq '/start']">
    <pipeline:pipeline>
      <pipeline:transformer name="start" xsl-path="start.xslt"/>
      <pipeline:transformer name="html-response" xsl-path="html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>

  <xsl:template match="/req:request[req:path eq '/operations']">
    <pipeline:pipeline>
      <pipeline:transformer name="archive-operations" xsl-path="archive-operations.xslt"/>
      <pipeline:transformer name="html-response" xsl-path="html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>

</xsl:stylesheet>
