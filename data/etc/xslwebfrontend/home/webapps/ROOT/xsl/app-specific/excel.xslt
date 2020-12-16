<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:json="http://www.w3.org/2005/xpath-functions"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:file="http://expath.org/ns/file"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:zip="http://www.armatiek.com/xslweb/zip-serializer"
    xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    xpath-default-namespace="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    exclude-result-prefixes="#all"
    expand-text="yes"
    version="3.0">
    
    <xsl:param name="config:webapp-dir" as="xs:string" required="yes"/>
    <xsl:param name="excel-preingest-dir" as="xs:string" required="yes"/>
    
    <xsl:include href="commonconstants.xslt"/>
    <xsl:include href="commoncode.xslt"/>
    
    <xsl:variable name="LETTERS" as="xs:string" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>
    
    <xsl:variable name="workdir-guid" as="xs:string" select="replace(/*/req:path, '^.*/([-a-z0-9]+)$', '$1')"/>
    
    <xsl:variable name="max-sheet-num" as="xs:integer" select="9"/>
    
    <xsl:mode on-no-match="shallow-copy"/>
    
    <xsl:function name="nha:excel-rownum" as="xs:string">
        <!-- Excel row 1 is rownum 0 -->
        <xsl:param name="rownum" as="xs:integer"/>
        <xsl:value-of select="$rownum + 1"/>
    </xsl:function>
    
    <xsl:function name="nha:excel-colnum" as="xs:string">
        <!-- Excel col A is colnum 0 -->
        <xsl:param name="colnum" as="xs:integer"/>
        <xsl:variable name="digit" as="xs:integer" select="$colnum mod 26"/>
        <xsl:variable name="rest" as="xs:integer" select="xs:integer($colnum div 26)"/>
        
        <xsl:variable name="result">
            <xsl:choose>
                <xsl:when test="$rest gt 0">{nha:excel-colnum($rest - 1) || substring($LETTERS, $digit + 1, 1)}</xsl:when>
                <xsl:otherwise>{substring($LETTERS, $digit + 1, 1)}</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        
        <xsl:value-of select="$result"/>
    </xsl:function>
    
    <xsl:function name="nha:cell-num" as="xs:string">
        <!-- Excel row 1 is rownum 0 -->
        <xsl:param name="rownum" as="xs:integer"/>
        <!-- Excel col A is colnum 0 -->
        <xsl:param name="colnum" as="xs:integer"/>
        
        <xsl:value-of select="nha:excel-colnum($colnum) || nha:excel-rownum($rownum)"/>
    </xsl:function>
    
    <xsl:function name="nha:row" as="element(row)">
        <!-- Excel row 1 is rownum 0 -->
        <xsl:param name="rownum" as="xs:integer"/>
        <xsl:param name="colvalues" as="xs:string+"/>
        <row r="{nha:excel-rownum($rownum)}" customFormat="false" ht="13.8" hidden="false" customHeight="false" outlineLevel="0" collapsed="false">
            <xsl:for-each select="$colvalues">
                <c r="{nha:cell-num($rownum, position() - 1)}" s="0" t="inlineStr"><is><t>{.}</t></is></c>
            </xsl:for-each>
        </row>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-1" as="element(row)*">
        <xsl:sequence select="nha:row(1, ('sessionId', 'TODO'))"/>
        <xsl:sequence select="nha:row(2, ('code', 'TODO'))"/>
        <xsl:sequence select="nha:row(3, ('actionName', 'TODO'))"/>
        <xsl:sequence select="nha:row(4, ('collectionItem', 'TODO'))"/>
        <xsl:sequence select="nha:row(5, ('message', 'TODO'))"/>
        <xsl:sequence select="nha:row(6, ('messages', 'TODO'))"/>
        <xsl:sequence select="nha:row(7, ('creationTimestamp', 'TODO'))"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-2" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'UnpackTarHandler.json'"/>
        
        <!-- TODO API call -->
        <xsl:variable name="jsonMap" as="map(*)" select="json-doc($nha:data-uri-prefix || $workdir-guid || '/' || $base-json-file)"/>
        
        <xsl:sequence select="nha:row(1, ('SessionId', string($jsonMap?SessionId)))"/>
        <xsl:sequence select="nha:row(2, ('Code',  string($jsonMap?Code)))"/>
        <xsl:sequence select="nha:row(3, ('ActionName', string($jsonMap?ActionName)))"/>
        <xsl:sequence select="nha:row(4, ('CollectionItem', string($jsonMap?CollectionItem)))"/>
        <xsl:sequence select="nha:row(5, ('Message', string($jsonMap?Message)))"/>
        <xsl:sequence select="nha:row(6, ('Messages', string($jsonMap?Messages)))"/>
        <xsl:sequence select="nha:row(7, ('CreationTimestamp', string($jsonMap?CreationTimestamp)))"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-commontype" as="element(row)*">
        <xsl:param name="base-json-file" as="xs:string"/>
        
        <!-- TODO API call -->
        <xsl:variable name="jsonArray" as="array(*)" select="json-doc($nha:data-uri-prefix || $workdir-guid || '/' || $base-json-file)"/>
        
        <xsl:for-each select="$jsonArray">
            <xsl:variable name="rownum" as="xs:integer" select="position()"/>
            <xsl:variable name="columnKeys" as="xs:string*" select="('SessionId', 'Code', 'ActionName', 'CollectionItem', 'Message', 'Messages', 'CreationTimestamp')"/>
            <xsl:variable name="columnValues" as="xs:string*">
                <xsl:for-each select="$columnKeys">{$jsonArray => array:get($rownum) => map:get(.)}</xsl:for-each>
            </xsl:variable>
            <xsl:sequence select="nha:row($rownum, $columnValues)"/>
        </xsl:for-each>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-sidecartype" as="element(row)*">
        <xsl:param name="excel-rownum" as="xs:integer"/>
        <xsl:param name="jsonArray" as="array(map(*))"/>
        
        <xsl:for-each select="1 to array:size($jsonArray)">
            <xsl:variable name="rownum" as="xs:integer" select="."/>
            <xsl:variable name="jsonMap" as="map(*)" select="$jsonArray => array:get($rownum)"/>
            <xsl:variable name="columnKeys" as="xs:string*" select="('sessionId', 'code', 'actionName', 'collectionItem', 'message', 'messages', 'creationTimestamp')"/>
            <xsl:variable name="columnValues" as="xs:string*">
                <xsl:for-each select="$columnKeys">
                    <xsl:value-of select="$jsonMap => map:get(.)"/>
                </xsl:for-each>
            </xsl:variable>
            <xsl:sequence select="nha:row($excel-rownum + $rownum, $columnValues)"/>
        </xsl:for-each>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-3" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'ScanVirusValidationHandler.json'"/>
        <xsl:sequence select="nha:worksheet-commontype($base-json-file)"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-4" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'NamingValidationHandler.json'"/>
        <xsl:sequence select="nha:worksheet-commontype($base-json-file)"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-5" as="element(row)*">

        <xsl:iterate select="('SidecarValidationHandler_Archief.json',
            'SidecarValidationHandler_Bestand.json',
            'SidecarValidationHandler_Dossier.json',
            'SidecarValidationHandler_Onbekend.json',
            'SidecarValidationHandler_Record.json',
            'SidecarValidationHandler_Samenvatting.json',
            'SidecarValidationHandler_Series.json')">
            <xsl:param name="start-rownum" as="xs:integer" select="0"/>
            <xsl:variable name="base-json-file" as="xs:string" select="."/>
            <!-- TODO API call -->
            <xsl:variable name="jsonArray" as="array(map(*))" select="json-doc($nha:data-uri-prefix || $workdir-guid || '/' || $base-json-file)"/>
            <xsl:sequence select="nha:worksheet-sidecartype($start-rownum, $jsonArray)"/>
            <xsl:next-iteration>
                <xsl:with-param name="start-rownum" select="$start-rownum + array:size($jsonArray)"/>
            </xsl:next-iteration>
        </xsl:iterate>
    </xsl:function>
    
    <xsl:template match="/req:request">
        <xsl:try>
            <zip:zip-serializer>
                <xsl:for-each select="file:list($config:webapp-dir || '/' || $excel-preingest-dir, true())">
                    <xsl:variable name="filename-in-zip" as="xs:string" select="."/>
                    <xsl:variable name="filename-on-disk" as="xs:string" select="$config:webapp-dir || '/' || $excel-preingest-dir || '/' || $filename-in-zip"/>
                    
                    <xsl:choose>
                        <!-- Skip folders: -->
                        <xsl:when test="not(file:is-file($filename-on-disk))"/>
                        <!-- Special treatment for the worksheet XML files: -->
                        <xsl:when test="matches($filename-in-zip, '[\\/]sheet\d+\.xml$')">
                            <xsl:variable name="sheetnum" as="xs:integer" select="xs:integer(replace($filename-in-zip, '^.*[\\/]sheet(\d+)\.xml$', '$1'))"/>
                            
                            <zip:inline-entry name="{$filename-in-zip}" method="xml" encoding="UTF-8" omit-xml-declaration="no" indent="no">
                                <xsl:apply-templates select="doc(file:path-to-uri($filename-on-disk))">
                                    <xsl:with-param name="sheetnum" select="$sheetnum" tunnel="yes"/>
                                </xsl:apply-templates>
                            </zip:inline-entry>
                        </xsl:when>
                        <xsl:otherwise>
                            <!-- Just let the file be copied to the excel/zip file: -->
                            <zip:file-entry name="{$filename-in-zip}" src="{$filename-on-disk}"/>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:for-each>
            </zip:zip-serializer>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
    <xsl:template match="sheetData">
        <xsl:param name="sheetnum" as="xs:integer" required="yes" tunnel="yes"/>
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:apply-templates select="row[1]"/>
            
            <xsl:choose>
                <xsl:when test="$sheetnum eq 1"><xsl:copy-of select="nha:worksheet-1()"/></xsl:when>
                <xsl:when test="$sheetnum eq 2"><xsl:copy-of select="nha:worksheet-2()"/></xsl:when>
                <xsl:when test="$sheetnum eq 3"><xsl:copy-of select="nha:worksheet-3()"/></xsl:when>
                <xsl:when test="$sheetnum eq 5"><xsl:copy-of select="nha:worksheet-5()"/></xsl:when>
                <xsl:otherwise>
                    <xsl:copy-of select="row[position() gt 1]"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
