<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:pipeline="http://www.armatiek.com/xslweb/pipeline"
  xmlns:config="http://www.armatiek.com/xslweb/configuration"
  xmlns:req="http://www.armatiek.com/xslweb/request"
  xmlns:log="http://www.armatiek.com/xslweb/functions/log"
  xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
  xmlns:err="http://www.w3.org/2005/xqt-errors"
  exclude-result-prefixes="#all" version="3.0" expand-text="yes">
  
  <xsl:param name="config:webapp-dir" required="yes"/>
  <xsl:param name="prewash-stylesheets-dir" required="yes"/>
  <xsl:param name="basename-prewash-default-stylesheet" required="yes"/>
  
  <xsl:variable name="DUMP_REQUEST" as="xs:boolean" static="yes" select="false()"/>

  <xsl:variable name="PREWASH" as="xs:string" select="'/prewash/'"/>
  <xsl:variable name="TOPX2XIP" as="xs:string" select="'/topx2xip/'"/>
  <xsl:variable name="TOPX2XIPFOLDER" as="xs:string" select="'/topx2xip-folder/'"/>
  <xsl:variable name="TOPX2HTML" as="xs:string" select="'/topx2html/'"/>
  <xsl:variable name="DROID2HTML" as="xs:string" select="'/droid2html/'"/>
  <xsl:variable name="PLANETS2HTML" as="xs:string" select="'/planets2html/'"/>
  <xsl:variable name="SHOWREQUESTXML" as="xs:string" select="'/request/'"/>
  
  <xsl:variable name="reluri" as="xs:string" select="replace(/*/req:path, '^/[^/]+/(.*)$', '$1')"/>
  
  <xsl:function name="nha:get-prewash-path" as="xs:string">
    <xsl:param name="optional-dir" as="xs:string?"/>
    <xsl:param name="basename" as="xs:string"/>
    
    <xsl:variable name="path" as="xs:string" select="if ($optional-dir) then $optional-dir || '/' || $basename else $basename"/>
    <xsl:value-of select="$path || '.xslt'"/>
  </xsl:function>
  
  <xsl:function name="nha:get-prewash-stylesheet" as="xs:string">
    <xsl:param name="prewash-from-request" as="xs:string"/>
    
    <xsl:variable name="xsltpath" as="xs:string" select="nha:get-prewash-path(encode-for-uri($prewash-stylesheets-dir), encode-for-uri($prewash-from-request))"/>
    <xsl:choose>
      <xsl:when test="doc-available($xsltpath)">
        <xsl:value-of select="$xsltpath"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:variable name="defaultpath" as="xs:string" select="nha:get-prewash-path((), $basename-prewash-default-stylesheet)"/>
        <xsl:variable name="errormessage" as="xs:string" select="'Could not open or parse prewash stylesheet &quot;' || $xsltpath || '&quot;'"/>
        <xsl:sequence select="log:log('ERROR', $errormessage)"/>
        <xsl:sequence select="error(xs:QName('nha:invalide-prewash-stylesheet'), $errormessage)"/> 
      </xsl:otherwise>
    </xsl:choose>
  </xsl:function>

  <xsl:variable name="conversions" as="xs:string+" select="($PREWASH, $TOPX2XIP, $TOPX2HTML, $DROID2HTML, $PLANETS2HTML, $TOPX2XIPFOLDER, $SHOWREQUESTXML)"/>
  
  <xsl:template match="/">
    <xsl:sequence select="file:write('/tmp/request.xml', /)" xmlns:file="http://expath.org/ns/file" use-when="$DUMP_REQUEST"/>
    <xsl:sequence select="log:log('INFO', 'Dealing with request-path ' || /req:request/req:path)"/>    
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $PREWASH)]">
    <pipeline:pipeline>
      <xsl:try>
        <xsl:variable name="prewash-from-request" as="xs:string" select="string(/*/req:parameters/req:parameter[@name eq 'prewash']/req:value)"/>
        <xsl:variable name="prewash-stylesheet" as="xs:string" select="nha:get-prewash-stylesheet($prewash-from-request)"/>
        
        <xsl:sequence select="log:log('INFO', 'Applying prewash stylesheet &quot;' || $prewash-stylesheet || '&quot;')"/>
        
        <pipeline:transformer name="prewash" xsl-path="{$prewash-stylesheet}">
          <pipeline:parameter name="prewash-stylesheet" type="xs:string">
            <pipeline:value>{$prewash-stylesheet}</pipeline:value>
          </pipeline:parameter>
        </pipeline:transformer>
        <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>

        <xsl:catch>
          <xsl:sequence select="log:log('INFO', 'An error occurred while trying to apply the prewash stylesheet')"/>
          <pipeline:transformer name="prewash-error" xsl-path="error.xslt">
            <pipeline:parameter name="message" type="xs:string">
              <pipeline:value>{$err:description}</pipeline:value>
            </pipeline:parameter>
            <pipeline:parameter name="error-code" type="xs:string">
              <pipeline:value>{$err:code}</pipeline:value>
            </pipeline:parameter>
          </pipeline:transformer>
        </xsl:catch>
      </xsl:try>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $TOPX2XIP)]">
    <pipeline:pipeline>
      <pipeline:transformer name="topx2xip" xsl-path="topx2xip.xslt"/>
      <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $TOPX2XIPFOLDER)]">
    <pipeline:pipeline>
      <pipeline:transformer name="topx2xip" xsl-path="topx2xip-folder.xslt"/>
      <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $TOPX2HTML)]">
    <pipeline:pipeline>
      <pipeline:transformer name="topx2html" xsl-path="topx2html.xslt"/>
      <pipeline:transformer name="html-response" xsl-path="html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $DROID2HTML)]">
    <pipeline:pipeline>
      <pipeline:transformer name="droid2html" xsl-path="droid2html.xslt"/>
      <pipeline:transformer name="html-response" xsl-path="html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $PLANETS2HTML)]">
    <pipeline:pipeline>
      <pipeline:transformer name="planet2html" xsl-path="planets2html.xslt"/>
      <pipeline:transformer name="html-response" xsl-path="html-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>
  
  <xsl:template match="/req:request[starts-with(req:path, $SHOWREQUESTXML)]">
    <pipeline:pipeline>
      <pipeline:transformer name="showrequest" xsl-path="show-request.xslt"/>
      <pipeline:transformer name="xml-response" xsl-path="xml-response.xslt"/>
    </pipeline:pipeline>
  </xsl:template>

  <xsl:template match="/req:request[not(replace(req:path, '^(/[^/]+/).*$', '$1') = $conversions)]">
    <pipeline:pipeline>
      <pipeline:transformer name="error" xsl-path="error.xslt">
        <pipeline:parameter name="message" type="xs:string">
          <pipeline:value>Geen transformatie gedefinieerd voor context-pad "{/req:request/req:webapp-path || /req:request/req:path}"</pipeline:value>
        </pipeline:parameter>
      </pipeline:transformer>
    </pipeline:pipeline>
  </xsl:template>
  
</xsl:stylesheet>
