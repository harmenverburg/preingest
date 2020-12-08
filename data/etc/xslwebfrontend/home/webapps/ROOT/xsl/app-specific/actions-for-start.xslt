<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0" expand-text="yes"
    xpath-default-namespace="http://www.w3.org/1999/xhtml" version="3.0">
    
    <!-- This stylesheet inspects the session in order to modify the value of setting of form elements. Certain conventions apply,
         for instance, the names should correspond with the request parameters and session keys. Also, some attributes are required,
         such as @value for <option> (unless the option is not selectable).
    -->

    <xsl:output method="xml" indent="no"/>

    <xsl:mode on-no-match="shallow-copy"/>

    <xsl:include href="commonconstants.xslt"/>
    <xsl:include href="commoncode.xslt"/>
    
    <xsl:template match="/">
        <xsl:variable name="sessionid" as="xs:string?" select="session:get-attribute($nha:sessionguid-key)"/>
        
        <xsl:choose>
            <xsl:when test="exists($sessionid)">
                <xsl:variable name="relative-path" as="xs:string?" select="nha:decode-uri(session:get-attribute($nha:selectedfile-field))"/>
                <xsl:variable name="checksum-type" as="xs:string" select="session:get-attribute($nha:checksumtype-field)"/>
                <xsl:variable name="checksum-value" as="xs:string" select="session:get-attribute($nha:checksumvalue-field)"/>
                
                <xsl:variable name="jsonfile-for-selected-archive-exists" as="xs:boolean" select="nha:jsonfile-for-selected-archive-exists($relative-path, $sessionid)"/>
                <xsl:variable name="checksum-in-json-file-matches" as="xs:boolean" select="if ($jsonfile-for-selected-archive-exists) then nha:checksum-in-json-file-matches($relative-path, $sessionid, $checksum-type, $checksum-value) else false()"/>
                
                <xsl:if test="$checksum-in-json-file-matches"><xsl:sequence select="session:set-attribute($nha:ongoing-action-key, ())"/></xsl:if>
                
                <xsl:message>jsonfile-for-selected-archive-exists="{$jsonfile-for-selected-archive-exists}"</xsl:message>
                <xsl:message>checksum-in-json-file-matches="{$checksum-in-json-file-matches}"</xsl:message>
                <xsl:apply-templates>
                    <xsl:with-param name="sessionid" as="xs:string" select="$sessionid" tunnel="yes"/>
                    <xsl:with-param name="jsonfile-for-selected-archive-exists" as="xs:boolean" select="$jsonfile-for-selected-archive-exists" tunnel="yes"/>
                    <xsl:with-param name="checksum-in-json-file-matches" as="xs:boolean" select="$checksum-in-json-file-matches" tunnel="yes"/>
                </xsl:apply-templates>
            </xsl:when>
            <xsl:otherwise>
                <xsl:copy-of select="/"/>
            </xsl:otherwise>
        </xsl:choose>        
    </xsl:template>
    
    <xsl:template name="disable-button">
        <xsl:param name="new-label" as="xs:string?" select="()"/>
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:attribute name="disabled" select="'disabled'"/>
            <xsl:choose>
                <xsl:when test="exists($new-label)"><xsl:value-of select="$new-label"/></xsl:when>
                <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
            </xsl:choose>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="button[not(@data-enablecondition)]">
        <xsl:choose>
            <xsl:when test="@name eq session:get-attribute($nha:ongoing-action-key)">
                <xsl:message>bij button name="{@name}"</xsl:message>
                <xsl:call-template name="disable-button"/>
            </xsl:when>
            <xsl:otherwise><xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy></xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template match="button[@data-enablecondition]">
        <xsl:param name="jsonfile-for-selected-archive-exists" as="xs:boolean" required="yes" tunnel="yes"/>
        <xsl:param name="checksum-in-json-file-matches" as="xs:boolean" required="yes" tunnel="yes"/>
        <xsl:message>4. ongoing="{session:get-attribute($nha:ongoing-action-key)}", @data-enablecondition="{@data-enablecondition}"</xsl:message>
        
        <xsl:choose>
            <xsl:when test="@data-enablecondition eq $nha:checksum-condition">
                <xsl:choose>
                    <xsl:when test="not($jsonfile-for-selected-archive-exists)">
                        <xsl:call-template name="disable-button"/>
                    </xsl:when>
                    <xsl:when test="not($checksum-in-json-file-matches)">
                        <xsl:call-template name="disable-button">
                            <xsl:with-param name="new-label" select="'checksum mismatch'"/>
                        </xsl:call-template>
                    </xsl:when>
                    <xsl:otherwise>
                        <!-- checksum matches, enable button. -->
                        <xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy>
                        <xsl:message>5. ongoing="{session:get-attribute($nha:ongoing-action-key)}"</xsl:message>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            
            <xsl:otherwise>
                <xsl:call-template name="disable-button"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
</xsl:stylesheet>
