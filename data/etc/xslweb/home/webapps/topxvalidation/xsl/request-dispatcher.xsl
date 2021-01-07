<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:pipeline="http://www.armatiek.com/xslweb/pipeline"
  xmlns:config="http://www.armatiek.com/xslweb/configuration"
  xmlns:req="http://www.armatiek.com/xslweb/request"
  xmlns:err="http://expath.org/ns/error"
  exclude-result-prefixes="#all" version="3.0" expand-text="yes">
  
  <xsl:param name="config:development-mode"/>
  <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
  <xsl:param name="data-uri-prefix-devmode" as="xs:string" required="yes"/>
  <xsl:param name="default-output-format" as="xs:string" select="'json'"/>
  
  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    
    <!-- Dit request-attribuut voorkomt dat we deze logica telkens moeten herhalen: -->
    <xsl:sequence select="req:set-attribute('data-uri-prefix', if ($config:development-mode) then $data-uri-prefix-devmode else $data-uri-prefix)"/>
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template name="xml-validation" match="/req:request[not(starts-with(req:path, '/validate-folder'))]">    
    <pipeline:pipeline>
      <pipeline:transformer name="generate-sample" xsl-path="load-file.xslt"/>   
      
      <!-- First validate using XML schema: -->
      <pipeline:schema-validator name="schema-validator" xsl-param-name="schema-validation-report">
        <pipeline:schema-paths>
          <pipeline:schema-path>ToPX-2.3_2.xsd</pipeline:schema-path>  
        </pipeline:schema-paths>
      </pipeline:schema-validator>
      
      <!-- Then validate using Schematron: -->
      <pipeline:schematron-validator name="schematron-validator" schematron-path="topx.sch" xsl-param-name="schematron-validation-report"/>
      
      <pipeline:transformer name="validation-report" xsl-path="validation-report-xml.xslt"/>
      
      <xsl:variable name="format-from-request" as="xs:string?" select="/*/req:parameters/req:parameter[@name eq 'format']/req:value"/>
      <xsl:variable name="format" as="xs:string" select="if ($format-from-request) then $format-from-request else $default-output-format"/>
      
      <xsl:choose>
        <xsl:when test="$format eq 'xml'">
          <pipeline:transformer name="response-xml" xsl-path="response-xml.xslt">
            <pipeline:parameter name="Content-Type" type="xs:string"><pipeline:value>application/xml</pipeline:value></pipeline:parameter>
          </pipeline:transformer>
        </xsl:when>
        <xsl:when test="$format eq 'html'">
          <pipeline:transformer name="validation-report-html" xsl-path="validation-report-html.xslt"/>
          <pipeline:transformer name="response-xhtml" xsl-path="response-xml.xslt">
            <pipeline:parameter name="Content-Type" type="xs:string"><pipeline:value>text/html</pipeline:value></pipeline:parameter>
          </pipeline:transformer>
        </xsl:when>
        <xsl:when test="$format eq 'json'">
          <pipeline:transformer name="validation-report-json" xsl-path="validation-report-json.xslt"/>
          <pipeline:transformer name="response-json" xsl-path="response-json.xslt"/>
        </xsl:when>
        
        <xsl:otherwise>
          <xsl:message>Invalid format: "{$format}"</xsl:message>
        </xsl:otherwise>
      </xsl:choose>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template name="validate-folder" match="/req:request[starts-with(req:path, '/validate-folder')]">    
    <pipeline:pipeline>
      <pipeline:transformer name="folder-validation-report" xsl-path="folder-validation-report-xml.xslt"/>
      
      <xsl:variable name="format-from-request" as="xs:string?" select="/*/req:parameters/req:parameter[@name eq 'format']/req:value"/>
      <xsl:variable name="format" as="xs:string" select="if ($format-from-request) then $format-from-request else $default-output-format"/>
      
      <xsl:choose>
        <xsl:when test="$format eq 'xml'">
          <pipeline:transformer name="response-xml" xsl-path="response-xml.xslt">
            <pipeline:parameter name="Content-Type" type="xs:string"><pipeline:value>application/xml</pipeline:value></pipeline:parameter>
          </pipeline:transformer>
        </xsl:when>
        <xsl:when test="$format eq 'html'">
          <pipeline:transformer name="folder-validation-report-html" xsl-path="folder-validation-report-html.xslt">
            <pipeline:parameter name="validation-detail-uri" type="xs:string"><pipeline:value>{/*/req:webapp-path}</pipeline:value></pipeline:parameter>
          </pipeline:transformer>
          <pipeline:transformer name="response-xhtml" xsl-path="response-xml.xslt">
            <pipeline:parameter name="Content-Type" type="xs:string"><pipeline:value>text/html</pipeline:value></pipeline:parameter>
          </pipeline:transformer>
        </xsl:when>
        <xsl:when test="$format eq 'json'">
          <pipeline:transformer name="folder-validation-report-json" xsl-path="folder-validation-report-json.xslt">
            <pipeline:parameter name="validation-detail-uri" type="xs:string"><pipeline:value>{/*/req:webapp-path}</pipeline:value></pipeline:parameter>
          </pipeline:transformer>
          <pipeline:transformer name="response-json" xsl-path="response-json.xslt"/>
        </xsl:when>
        
        <xsl:otherwise>
          <xsl:message>Invalid format: "{$format}"</xsl:message>
        </xsl:otherwise>
      </xsl:choose>
    </pipeline:pipeline>
  </xsl:template>
</xsl:stylesheet>
