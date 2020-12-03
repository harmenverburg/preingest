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
    
    <xsl:template match="textarea">
        <xsl:variable name="textarea-name" as="xs:string?" select="@name"/>
        <xsl:variable name="sessionvalue" as="xs:string" select="if ($textarea-name) then session:get-attribute($textarea-name) else ''"/>
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:value-of select="$sessionvalue"/>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="input[@type eq 'radio']">
        <xsl:variable name="input-name" as="xs:string?" select="@name"/>
        <xsl:variable name="fieldvalue" as="xs:string?" select="@value"/>
        <xsl:variable name="sessionvalue" as="xs:string?" select="if ($input-name) then session:get-attribute($input-name) else ()"/>
        
        <xsl:copy>
            <xsl:apply-templates select="@* except @checked"/>
            <xsl:if test="exists($fieldvalue) and $fieldvalue eq $sessionvalue">
                <xsl:attribute name="checked" select="'checked'"/>
            </xsl:if>
            <xsl:apply-templates/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="option">
        <xsl:variable name="select-name" as="xs:string?" select="parent::select/@name"/>
        <xsl:variable name="fieldvalue" as="xs:string?" select="@value"/>
        <xsl:variable name="sessionvalue" as="xs:string?" select="if ($select-name) then session:get-attribute($select-name) else ()"/>

        <xsl:copy>
            <xsl:apply-templates select="@* except @selected"/>
            <xsl:if test="exists($fieldvalue) and $fieldvalue eq $sessionvalue">
                <xsl:attribute name="selected" select="'selected'"/>
            </xsl:if>
            <xsl:apply-templates/>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template name="disable-button">
        <xsl:param name="new-label" as="xs:string?" select="()"/>
        <xsl:copy>
            <xsl:apply-templates select="@* except @data-enablecondition"/>
            <xsl:attribute name="disabled" select="'disabled'"/>
            <xsl:choose>
                <xsl:when test="exists($new-label)"><xsl:value-of select="$new-label"/></xsl:when>
                <xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
            </xsl:choose>
        </xsl:copy>
    </xsl:template>
    
    <xsl:template match="button[@data-enablecondition]">
        <xsl:variable name="sessionid" as="xs:string?" select="session:get-attribute($nha:sessionguid-key)"/>
        <xsl:choose>
            <xsl:when test="exists($sessionid) and @data-enablecondition eq $nha:checksum-condition">
                <xsl:variable name="relative-path" as="xs:string?" select="nha:decode-uri(session:get-attribute($nha:selectedfile-field))"/>
                <xsl:variable name="checksum-type" as="xs:string" select="session:get-attribute($nha:checksumtype-field)"/>
                <xsl:variable name="checksum-value" as="xs:string" select="session:get-attribute($nha:checksumvalue-field)"/>
                
                <xsl:choose>
                    <xsl:when test="not(nha:jsonfile-for-selected-archive-exists($relative-path, $sessionid))">
                        <xsl:call-template name="disable-button"/>
                    </xsl:when>
                    <xsl:when test="not(nha:checksum-in-json-file-matches($relative-path, $sessionid, $checksum-type, $checksum-value))">
                        <xsl:call-template name="disable-button">
                            <xsl:with-param name="new-label" select="'checksum mismatch'"/>
                        </xsl:call-template>
                    </xsl:when>
                    <xsl:otherwise>
                        <!-- checksum matches, enable button. -->
                        <xsl:copy><xsl:apply-templates select="@* | node()"/></xsl:copy>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="disable-button"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
</xsl:stylesheet>
