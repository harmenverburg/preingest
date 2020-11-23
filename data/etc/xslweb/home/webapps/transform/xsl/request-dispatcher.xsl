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
  
  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:variable name="TOPX2XIP" as="xs:string" select="'/topx2xip'"/>
  <xsl:variable name="TOPX2XIPFOLDER" as="xs:string" select="'/topx2xip-folder'"/>
  <xsl:variable name="SHOWREQUESTXML" as="xs:string" select="'/request'"/>

  <xsl:variable name="conversions" as="xs:string+" select="($TOPX2XIP, $TOPX2XIPFOLDER, $SHOWREQUESTXML)"/>
  
  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    
    <!-- Dit request-attribuut voorkomt dat we deze logica telkens moeten herhalen: -->
    <xsl:sequence select="req:set-attribute('data-uri-prefix', if ($config:development-mode) then $data-uri-prefix-devmode else $data-uri-prefix)"/>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="/req:request[req:path eq $TOPX2XIP]">
    <xsl:call-template name="parameter-based-template">
      <xsl:with-param name="paramname" select="'reluri'"/>
      <xsl:with-param name="stylesheet" select="'topx2xip.xslt'"></xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
  <xsl:template match="/req:request[req:path eq $TOPX2XIPFOLDER]">
    <xsl:call-template name="parameter-based-template">
      <xsl:with-param name="paramname" select="'reluri'"/>
      <xsl:with-param name="stylesheet" select="'topx2xip-folder.xslt'"></xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
  <xsl:template match="/req:request[req:path eq $SHOWREQUESTXML]">
    <pipeline:pipeline>
      <pipeline:transformer name="showrequest" xsl-path="show-request.xslt"/>
      <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>

  <xsl:template match="/req:request[not(req:path = $conversions)]">
    <pipeline:pipeline>
      <pipeline:transformer name="error" xsl-path="error.xslt">
        <pipeline:parameter name="message" type="xs:string">
          <pipeline:value>Geen transformatie gedefinieerd voor context-pad
            "{/req:request/req:webapp-path || /req:request/req:path}"</pipeline:value>
        </pipeline:parameter>
      </pipeline:transformer>
    </pipeline:pipeline>
  </xsl:template>

  <xsl:template name="parameter-based-template">
    <xsl:param name="paramname" as="xs:string" required="yes"/>
    <xsl:param name="stylesheet" as="xs:string"/>
    <pipeline:pipeline>
      <xsl:choose>
        <xsl:when test="not(/*/req:parameters/req:parameter[@name eq $paramname]/req:value)">
          <pipeline:transformer name="error" xsl-path="error.xslt">
            <pipeline:parameter name="message" type="xs:string">
              <pipeline:value>Request-parameter "{$paramname}" ontbreekt voor context-pad "{/req:request/req:webapp-path ||
                /req:request/req:path}". Het moet verwijzen naar een bestandsuri relatief t.o.v. de folder "{req:get-attribute('data-uri-prefix')}" in de Dockercontainer.</pipeline:value>
            </pipeline:parameter>
            <pipeline:parameter name="error-code" type="xs:string">
              <pipeline:value>missing-parameter</pipeline:value>
            </pipeline:parameter>
          </pipeline:transformer>
        </xsl:when>
        <xsl:otherwise>
          <pipeline:transformer name="topx2xip" xsl-path="{$stylesheet}"/>
          <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>
        </xsl:otherwise>
      </xsl:choose>
    </pipeline:pipeline>
  </xsl:template>
  
</xsl:stylesheet>
