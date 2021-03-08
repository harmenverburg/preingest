<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:resp="http://www.armatiek.com/xslweb/response"
    xmlns:config="http://www.armatiek.com/xslweb/configuration"
    xmlns:file="http://expath.org/ns/file"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:validation="http://www.armatiek.com/xslweb/validation"
    xmlns:svrl="http://purl.oclc.org/dsdl/svrl"
    expand-text="yes"
    default-mode="topx2xip-folder"
    version="3.0">
    
    <xsl:param name="data-uri-prefix" as="xs:string" required="yes"/>
    
    <xsl:variable name="context-path" select="/*/req:context-path || /*/req:webapp-path" as="xs:string"/>
    
    <!-- Wrapper function for non-standard call to discard-document() -->
    <xsl:function name="nha:discard-document" as="document-node()">
        <xsl:param name="doc" as="document-node()"/>
        <!-- Functie saxon:discard-document() is niet beschikbaar in de Saxon Home-editie. Het kan enorm op geheugen besparen als er heel veel XML-files zijn.
             N.B. bij de home-editie geeft function-avaible toch true() terug, dus onderstaande werkt niet:
             
             <xsl:sequence select="if (function-available('saxon:discard-document')) then saxon:discard-document($doc) else $doc"/>
             
             Maar gelukkig heeft XSLWeb zijn eigen discard-document(), dus in de context daarvan kunnen we die gebruiken:
        -->
        <xsl:sequence select="util:discard-document($doc)" xmlns:util="http://www.armatiek.com/xslweb/functions/util"/>
    </xsl:function>
    
    <xsl:template match="/" mode="topx2xip-folder">
        <xsl:try>
            <xsl:variable name="reluri" as="xs:string" select="encode-for-uri(substring-after(/*/req:path, '/validate-folder/'))"/>
            <xsl:variable name="metadatafiles" as="xs:string*" select="file:list($data-uri-prefix || $reluri, true(), '*.metadata')"/>
            
            <nha:folder-validation data-uri-prefix="{$data-uri-prefix}" reluri="{$reluri}">
                <xsl:for-each select="$metadatafiles">
                    <xsl:variable name="validation-uri" as="xs:string" select="'xslweb://' || $context-path || '/' || $reluri || '/' || encode-for-uri(.) || '?format=xml'"/>
                    
                    <xsl:variable name="valresult" as="document-node()" select="nha:discard-document(doc($validation-uri))"/>
                    <xsl:variable name="count-schema-errors" as="xs:integer" select="count($valresult/*/nha:schema-validation-report//validation:error)"/>
                    <xsl:variable name="count-schematron-errors" as="xs:integer" select="count($valresult/*/nha:schematron-validation-report//svrl:failed-assert)"/>
                    
                    <xsl:if test="$count-schema-errors ne 0 or $count-schematron-errors ne 0">
                        <nha:file-with-errors reluri="{replace($reluri || '/' || encode-for-uri(.), '%2F', '/', 'i')}" count-schema-errors="{$count-schema-errors}" count-schematron-errors="{$count-schematron-errors}"/>
                    </xsl:if>
                </xsl:for-each>
            </nha:folder-validation>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
</xsl:stylesheet>
