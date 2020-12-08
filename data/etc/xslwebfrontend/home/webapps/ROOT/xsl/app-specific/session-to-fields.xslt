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
        <xsl:variable name="sessionvalue" as="xs:string?" select="if ($textarea-name) then session:get-attribute($textarea-name) else ()"/>
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
    
</xsl:stylesheet>
