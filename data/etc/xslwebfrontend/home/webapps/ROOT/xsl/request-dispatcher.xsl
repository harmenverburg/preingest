<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:pipeline="http://www.armatiek.com/xslweb/pipeline"
  xmlns:config="http://www.armatiek.com/xslweb/configuration"
  xmlns:req="http://www.armatiek.com/xslweb/request"
  xmlns:zip="http://www.armatiek.com/xslweb/zip-serializer"
  xmlns:err="http://expath.org/ns/error"
  xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
  exclude-result-prefixes="#all" version="3.0" expand-text="yes">

  <xsl:param name="config:development-mode"/>
  <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
  <xsl:param name="data-uri-prefix-devmode" as="xs:string" required="yes"/>
  
  <xsl:include href="app-specific/commonconstants.xslt"/>
  
  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    
    <xsl:variable name="context-path" select="/*/req:context-path || /*/req:webapp-path" as="xs:string"/>
    <xsl:variable name="actions-uri-prefix" as="xs:string" select="$context-path || '/actions'"/>      
    
    <xsl:variable name="data-uri-prefix" as="xs:string" select="if ($config:development-mode) then $data-uri-prefix-devmode else $data-uri-prefix"/>

    <!-- These request attributes serve to prevent duplication of code: -->
    <xsl:sequence select="req:set-attribute($nha:context-path-key, $context-path)"/>
    <xsl:sequence select="req:set-attribute($nha:data-uri-prefix-key, $data-uri-prefix)"/>
    <xsl:sequence select="req:set-attribute($nha:actions-uri-prefix-key, $actions-uri-prefix)"/>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="/req:request[req:path eq '/' or req:path eq '/start']">
    <pipeline:pipeline>
      <pipeline:transformer name="requestparameters-to-session" xsl-path="app-specific/requestparameters-to-session.xslt"/>
      
      <pipeline:transformer name="start" xsl-path="app-specific/start.xslt"/>
      
      <pipeline:transformer name="session-to-fields" xsl-path="app-specific/session-to-fields.xslt"/>
      
      <pipeline:transformer name="html-response" xsl-path="app-specific/html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>

  <xsl:template match="/req:request[req:path eq '/operations']">
    <pipeline:pipeline>
      <pipeline:transformer name="requestparameters-to-session" xsl-path="app-specific/requestparameters-to-session.xslt"/>
      
      <pipeline:transformer name="operations" xsl-path="app-specific/operations.xslt"/>
      
      <pipeline:transformer name="session-to-fields" xsl-path="app-specific/session-to-fields.xslt"/>
      <pipeline:transformer name="html-response" xsl-path="app-specific/html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, '/actions')]">
    <pipeline:pipeline>
      <pipeline:transformer name="actions" xsl-path="app-specific/actions.xslt"/>
    </pipeline:pipeline>
  </xsl:template>

  <xsl:template match="/req:request[starts-with(req:path, '/excel/')]">
    <!-- After /excel/, place the guid of the directory containing the generated JSON files -->
    <pipeline:pipeline>
      <pipeline:transformer name="actions" xsl-path="app-specific/excel.xslt"/>
      <pipeline:transformer name="xlsx-response" xsl-path="app-specific/xlsx-response.xslt"/>
      <pipeline:zip-serializer name="zip"/>
    </pipeline:pipeline>
  </xsl:template>
  
</xsl:stylesheet>
