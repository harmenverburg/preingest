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

  <xsl:param name="output-api-basepath" as="xs:string" required="yes"/>
  
  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, '/')]">
    <!-- After /excel/, place the guid of the directory containing the generated JSON files -->
    <pipeline:pipeline>
      <pipeline:transformer name="excelreport" xsl-path="app-specific/excelreport.xslt"/>
      <pipeline:transformer name="xlsx-response" xsl-path="app-specific/xlsx-response.xslt"/>
      <pipeline:zip-serializer name="zip"/>
    </pipeline:pipeline>
  </xsl:template>
  
</xsl:stylesheet>
