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
    
    <!-- This stylesheet stores all request parameters with an identical name/key into the session. -->

    <xsl:output method="xml" indent="no"/>

    <xsl:template match="/">
        <!-- Grouping does not seem necessary, as the parameters are already grouped as in the following sample:
            <req:parameters>
                <req:parameter name="aap">
                    <req:value>noot</req:value>
                    <req:value>mies</req:value>
                    <req:value>jet</req:value>
                </req:parameter>
                <req:parameter name="wim">
                    <req:value>zus</req:value>
                </req:parameter>
            </req:parameters>
        -->
        <xsl:for-each-group select="/*/req:parameters/req:parameter" group-by="@name">
            <xsl:sequence select="session:set-attribute(current-grouping-key(), current-group()/req:value)"/>
        </xsl:for-each-group>

        <xsl:copy-of select="/"/>
    </xsl:template>
</xsl:stylesheet>
