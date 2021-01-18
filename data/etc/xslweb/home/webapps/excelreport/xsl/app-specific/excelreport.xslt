<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:session="http://www.armatiek.com/xslweb/session"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:log="http://www.armatiek.com/xslweb/functions/log"
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
    <xsl:param name="preingest-scheme-host-port" as="xs:string" required="yes"/>
    <xsl:param name="output-api-basepath" as="xs:string" required="yes"/>
    <xsl:param name="excel-preingest-dir" as="xs:string" required="yes"/>
    
    <xsl:variable name="context-path" select="/*/req:context-path || /*/req:webapp-path" as="xs:string"/> 
    
    <xsl:variable name="LETTERS" as="xs:string" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>
    
    <xsl:variable name="COLUMNKEYS1" as="xs:string*" select="('SessionId', 'Code', 'ActionName', 'CollectionItem', 'Message', 'Messages', 'CreationTimestamp')"/>
    <xsl:variable name="COLUMNKEYS2" as="xs:string*" select="('sessionId', 'code', 'actionName', 'collectionItem', 'message', 'messages', 'creationTimestamp')"/>
    
    <xsl:variable name="workdir-guid" as="xs:string" select="replace(/*/req:path, '^.*/([-a-z0-9]+)$', '$1')"/>
    
    <xsl:variable name="jsondoc-uri-prefix" as="xs:string" select="$preingest-scheme-host-port || $output-api-basepath || '/json/' || $workdir-guid || '/'"/>
    <xsl:variable name="report-uri-prefix" as="xs:string" select="$preingest-scheme-host-port || $output-api-basepath || '/report/' || $workdir-guid || '/'"/>
    
    <xsl:mode on-no-match="shallow-copy"/>
    
    <xsl:function name="nha:excel-rownum" as="xs:string">
        <!-- Excel row 1 is rownum 0 -->
        <xsl:param name="excel-rownum" as="xs:integer"/>
        <xsl:value-of select="$excel-rownum + 1"/>
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
        <xsl:param name="excel-rownum" as="xs:integer"/>
        <!-- Excel col A is colnum 0 -->
        <xsl:param name="colnum" as="xs:integer"/>
        
        <xsl:value-of select="nha:excel-colnum($colnum) || nha:excel-rownum($excel-rownum)"/>
    </xsl:function>
    
    <xsl:function name="nha:row" as="element(row)">
        <!-- Excel row 1 is rownum 0 -->
        <xsl:param name="excel-rownum" as="xs:integer"/>
        <xsl:param name="colvalues" as="xs:string+"/>
        <row r="{nha:excel-rownum($excel-rownum)}" customFormat="false" ht="13.8" hidden="false" customHeight="false" outlineLevel="0" collapsed="false">
            <xsl:for-each select="$colvalues">
                <c r="{nha:cell-num($excel-rownum, position() - 1)}" s="0" t="inlineStr"><is><t>{.}</t></is></c>
            </xsl:for-each>
        </row>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-jsonmap" as="element(row)*">
        <xsl:param name="excel-rownum" as="xs:integer"/>
        <xsl:param name="json-rownum" as="xs:integer"/>
        <xsl:param name="jsonMap" as="map(*)"/>
        <xsl:param name="columnKeys" as="xs:string*"/>
        
        <xsl:variable name="columnValues" as="xs:string*">
            <xsl:for-each select="$columnKeys">
                <xsl:value-of select="$jsonMap => map:get(.)"/>
            </xsl:for-each>
        </xsl:variable>
        <xsl:sequence select="nha:row($excel-rownum + $json-rownum, $columnValues)"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-jsonarray" as="element(row)*">
        <xsl:param name="excel-rownum" as="xs:integer"/>
        <xsl:param name="jsonArray" as="array(map(*))"/>
        <xsl:param name="columnKeys" as="xs:string*"/>
        
        <xsl:choose>
            <xsl:when test="array:size($jsonArray) eq 0">
                <xsl:sequence select="nha:row($excel-rownum + 0, ('Geen bijzonderheden'))"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:for-each select="1 to array:size($jsonArray)">
                    <xsl:variable name="json-rownum" as="xs:integer" select="."/>
                    <xsl:variable name="jsonMap" as="map(*)" select="$jsonArray => array:get($json-rownum)"/>
                    
                    <xsl:sequence select="nha:worksheet-jsonmap($excel-rownum, $json-rownum - 1, $jsonMap, $columnKeys)"/>
                </xsl:for-each>
                
            </xsl:otherwise>
        </xsl:choose>        
    </xsl:function>
    
    <xsl:function name="nha:worksheet-commontype" as="element(row)*">
        <xsl:param name="base-json-file" as="xs:string"/>
        
        <xsl:try>
            <xsl:variable name="json-uri" as="xs:string" select="$jsondoc-uri-prefix || $base-json-file"/>
            <xsl:sequence select="log:log('INFO', 'Accessing JSON-uri ' || $json-uri)"/>
            <xsl:variable name="jsonArray" as="array(map(*))" select="json-doc($json-uri)"/>
            
            <xsl:sequence select="nha:worksheet-jsonarray(1, $jsonArray, $COLUMNKEYS1)"/> <!-- Start at row 1 (Excel 2) because the top row is a header row. -->
            <xsl:catch>
                <xsl:sequence select="log:log('ERROR', 'Error code: ' || $err:code || ', description: ' || $err:description || ', at line ' || $err:line-number || ' of module ' || $err:module)"/>
            </xsl:catch>
        </xsl:try>        
    </xsl:function>
    
    <xsl:function name="nha:worksheet-1" as="element(row)*">
        <xsl:try>
            <xsl:sequence select="nha:row(1, ('sessionId', 'TODO'))"/>
            <xsl:sequence select="nha:row(2, ('code', 'TODO'))"/>
            <xsl:sequence select="nha:row(3, ('actionName', 'TODO'))"/>
            <xsl:sequence select="nha:row(4, ('collectionItem', 'TODO'))"/>
            <xsl:sequence select="nha:row(5, ('message', 'TODO'))"/>
            <xsl:sequence select="nha:row(6, ('messages', 'TODO'))"/>
            <xsl:sequence select="nha:row(7, ('creationTimestamp', 'TODO'))"/>
            <xsl:catch>
                <xsl:sequence select="log:log('ERROR', 'Error code: ' || $err:code || ', description: ' || $err:description || ', at line ' || $err:line-number || ' of module ' || $err:module)"/>
            </xsl:catch>
        </xsl:try>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-2" as="element(row)*">
        <xsl:try>
            <xsl:variable name="base-json-file" as="xs:string" select="'UnpackTarHandler.json'"/>
            <xsl:variable name="json-uri" as="xs:string" select="$jsondoc-uri-prefix || $base-json-file"/>
            <xsl:sequence select="log:log('INFO', 'Accessing JSON-uri ' || $json-uri)"/>
            <xsl:variable name="jsonMap" as="map(*)" select="json-doc($json-uri)"/>
            
            <xsl:sequence select="nha:row(1, ('SessionId', $jsonMap?SessionId))"/>
            <xsl:sequence select="nha:row(2, ('Code', $jsonMap?Code))"/>
            <xsl:sequence select="nha:row(3, ('ActionName', $jsonMap?ActionName))"/>
            <xsl:sequence select="nha:row(4, ('CollectionItem', $jsonMap?CollectionItem))"/>
            <xsl:sequence select="nha:row(5, ('Message', $jsonMap?Message))"/>
            <xsl:sequence select="nha:row(6, ('Messages', $jsonMap?Messages))"/>
            <xsl:sequence select="nha:row(7, ('CreationTimestamp', $jsonMap?CreationTimestamp))"/>
            <xsl:catch>
                <xsl:sequence select="log:log('ERROR', 'Error code: ' || $err:code || ', description: ' || $err:description || ', at line ' || $err:line-number || ' of module ' || $err:module)"/>
            </xsl:catch>
        </xsl:try>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-3" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'ScanVirusValidationHandler.json'"/>
        <!-- TODO test with real data -->
        <xsl:sequence select="nha:worksheet-commontype($base-json-file)"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-4" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'NamingValidationHandler.json'"/>
        <xsl:sequence select="nha:worksheet-commontype($base-json-file)"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-5" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'MetadataValidationHandler.json'"/>
        <xsl:sequence select="nha:worksheet-commontype($base-json-file)"/>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-6" as="element(row)*">
        <!-- Deze functie wacht op issue # 40 "Same JSON-format for all SidecarValidationHandler" -->
        <xsl:try>
            <xsl:iterate select="(
                'SidecarValidationHandler_Samenvatting.json',
                'SidecarValidationHandler_Archief.json',
                'SidecarValidationHandler_Series.json',
                'SidecarValidationHandler_Dossier.json',
                'SidecarValidationHandler_Record.json',
                'SidecarValidationHandler_Bestand.json',
                'SidecarValidationHandler_Onbekend.json'
                )">
                <xsl:param name="start-rownum" as="xs:integer" select="1"/> <!-- Row 0 (Excel 1) is header row -->
                <xsl:variable name="base-json-file" as="xs:string" select="."/>
                <xsl:variable name="json-uri" as="xs:string" select="$jsondoc-uri-prefix || $base-json-file"/>
                <xsl:variable name="jsonArrayOrMap" select="json-doc($json-uri)"/>
                <xsl:variable name="isMap" as="xs:boolean" select="$jsonArrayOrMap instance of map(*)"/>
                <xsl:sequence select="log:log('INFO', 'Accessing JSON-uri ' || $json-uri || ', isMap=' || $isMap || ', starting at row # ' || $start-rownum || ' (Excel row # ' || $start-rownum + 1 || ')')"/>
                <xsl:choose>
                    <xsl:when test="$isMap">
                        <xsl:sequence select="nha:worksheet-jsonmap($start-rownum, 0, $jsonArrayOrMap, $COLUMNKEYS2)"/>
                    </xsl:when>
                    <xsl:otherwise>
                        <!-- Array -->
                        <xsl:sequence select="nha:worksheet-jsonarray($start-rownum, $jsonArrayOrMap, $COLUMNKEYS2)"/>
                    </xsl:otherwise>
                </xsl:choose>
                
                <xsl:variable name="increment" as="xs:integer" select="if ($isMap) then 1 else array:size($jsonArrayOrMap)"/>

                <xsl:next-iteration>
                    <xsl:with-param name="start-rownum" select="$start-rownum + $increment"/>
                </xsl:next-iteration>
            </xsl:iterate>
            <xsl:catch>
                <xsl:sequence select="log:log('ERROR', 'Error code: ' || $err:code || ', description: ' || $err:description || ', at line ' || $err:line-number || ' of module ' || $err:module)"/>
            </xsl:catch>
        </xsl:try>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-7" as="element(row)*">
        <xsl:try>
            <xsl:variable name="csv-file" as="xs:string" select="$report-uri-prefix || 'DroidValidationHandler.csv'"/>
            <xsl:sequence select="log:log('INFO', 'Accessing CSV-uri ' || $csv-file)"/>
            <xsl:variable name="csv-lines" as="xs:string" select="unparsed-text($csv-file)"/>
            <xsl:for-each select="tokenize($csv-lines, '&#10;')[position() gt 1]">
                <xsl:variable name="rownum" as="xs:integer" select="position()"/>
                <xsl:variable name="csv-line" as="xs:string" select="."/>
                <xsl:variable name="columnValues" as="xs:string*">
                    <xsl:analyze-string select="$csv-line" regex="&quot;([^&quot;]*)&quot;">
                        <xsl:matching-substring>
                            <xsl:value-of select="regex-group(1)"/>
                        </xsl:matching-substring>
                        <xsl:non-matching-substring/>
                    </xsl:analyze-string>
                </xsl:variable>
                
                <xsl:if test="exists($columnValues)"><xsl:sequence select="nha:row($rownum, $columnValues)"/></xsl:if>
            </xsl:for-each>
            <xsl:catch>
                <xsl:sequence select="log:log('ERROR', 'Error code: ' || $err:code || ', description: ' || $err:description || ', at line ' || $err:line-number || ' of module ' || $err:module)"/>
            </xsl:catch>
        </xsl:try>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-8" as="element(row)*">
        <xsl:try>
            <xsl:variable name="base-json-file" as="xs:string" select="'GreenListHandler.json'"/>
            <xsl:variable name="json-uri" as="xs:string" select="$jsondoc-uri-prefix || $base-json-file"/>
            <xsl:sequence select="log:log('INFO', 'Accessing JSON-uri ' || $json-uri)"/>
            <xsl:variable name="jsonArray" as="array(map(*))" select="json-doc($json-uri)"/>
            <!-- Start at row 1 (Excel 2), because the first row is a header row. -->
            <xsl:sequence select="nha:worksheet-jsonarray(1, $jsonArray, ('SessionId', 'Location', 'Name', 'Extension', 'FormatName', 'FormatVersion', 'Puid', 'InGreenList'))"/>
            <xsl:catch>
                <xsl:sequence select="log:log('ERROR', 'Error code: ' || $err:code || ', description: ' || $err:description || ', at line ' || $err:line-number || ' of module ' || $err:module)"/>
            </xsl:catch>
        </xsl:try>
    </xsl:function>
    
    <xsl:function name="nha:worksheet-9" as="element(row)*">
        <xsl:variable name="base-json-file" as="xs:string" select="'EncodingHandler.json'"/>
        <xsl:sequence select="nha:worksheet-commontype($base-json-file)"/>
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
                            
                            <xsl:sequence select="log:log('INFO', 'Accessing file inside Excel-zip ' || $filename-in-zip)"/>
                            
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
    
    <!-- Remove column definitions like <col min="1026" max="16384" width="9.140625" style="3"/>, assuming that the value of min identifies them: -->
    <xsl:template match="col[xs:integer(@min) ge 100]"/>
    
    <xsl:template match="sheetData">
        <xsl:param name="sheetnum" as="xs:integer" required="yes" tunnel="yes"/>
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:apply-templates select="row[1]"/>
            
            <!--<xsl:message>Dealing with worksheet # {$sheetnum}</xsl:message>-->
            <xsl:sequence select="log:log('INFO', 'Dealing with worksheet # ' || $sheetnum)"/>
            <xsl:choose>
                <xsl:when test="$sheetnum eq 1"><xsl:copy-of select="nha:worksheet-1()"/></xsl:when>
                <xsl:when test="$sheetnum eq 2"><xsl:copy-of select="nha:worksheet-2()"/></xsl:when>
                <xsl:when test="$sheetnum eq 3"><xsl:copy-of select="nha:worksheet-3()"/></xsl:when>
                <xsl:when test="$sheetnum eq 4"><xsl:copy-of select="nha:worksheet-4()"/></xsl:when>
                <xsl:when test="$sheetnum eq 5"><xsl:copy-of select="nha:worksheet-5()"/></xsl:when>
                <xsl:when test="$sheetnum eq 6"><xsl:copy-of select="nha:worksheet-6()"/></xsl:when>
                <xsl:when test="$sheetnum eq 7"><xsl:copy-of select="nha:worksheet-7()"/></xsl:when>
                <xsl:when test="$sheetnum eq 8"><xsl:copy-of select="nha:worksheet-8()"/></xsl:when>
                <xsl:when test="$sheetnum eq 9"><xsl:copy-of select="nha:worksheet-9()"/></xsl:when>
                <xsl:otherwise>
                    <xsl:copy-of select="row[position() gt 1]"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
