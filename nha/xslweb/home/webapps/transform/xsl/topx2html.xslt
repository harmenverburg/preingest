<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    xmlns:err="http://www.w3.org/2005/xqt-errors"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    expand-text="yes"
    version="3.0">
    
    <xsl:output method="html" version="5"/>
    
    <xsl:variable name="data-uri-prefix" as="xs:string" select="req:get-attribute('data-uri-prefix')"/>
    
    <xsl:variable name="reluri" as="xs:string" select="replace(/*/req:path, '^/[^/]+/(.*)$', '$1')"/>
    <xsl:variable name="full-html" as="xs:string" select="string(/*/req:parameters/req:parameter[@name eq 'full-html']/req:value)"/>
    
    <xsl:template match="/">
        <xsl:try>
            <xsl:choose>
                <xsl:when test="$full-html eq 'true'">
                    <html>
                        <head>
                            <title>ToPX</title>
                            <style type="text/css" xsl:expand-text="no">
                                dl.leaf &gt; dd {
                                    display: inline;
                                    margin: 0;
                                }
                                dl.leaf &gt; dd:after {
                                    display: inline-block;
                                    content: ' ';
                                }
                                dt {
                                    font-weight: bold;
                                }
                                dl.container &gt; dt {
                                    font-size: larger;
                                    color: darkblue;
                                }
                                dl.leaf &gt; dt {
                                    display: inline-block;
                                }
                                dl.leaf &gt; dt:after {
                                    content: ': ';
                                }
                            </style>
                        </head>
                        <body>
                            <xsl:apply-templates select="doc($data-uri-prefix || encode-for-uri($reluri))/*"/>
                        </body>
                    </html>
                </xsl:when>
                <xsl:otherwise>
                    <div>
                        <xsl:apply-templates select="doc($data-uri-prefix || encode-for-uri($reluri))/*"/>
                    </div>
                </xsl:otherwise>
            </xsl:choose>
            <xsl:catch>
                <nha:error code="{$err:code}" description="{$err:description}" module="{$err:module}" line-number="{$err:line-number}"/>
            </xsl:catch>
        </xsl:try>
    </xsl:template>
    
    <xsl:template match="*[node()]">
        <dl class="{if (count(node()) eq count(text())) then 'leaf' else 'container'}">
            <dt>{local-name()}</dt>
            <dd><xsl:apply-templates/></dd>
        </dl>
    </xsl:template>
    
    <xsl:template match="*[not(node())]">
        <dl class="leaf">
            <dt>{local-name()}</dt>
            <dd>--</dd>
        </dl>
    </xsl:template>
    
    <xsl:template match="text()[normalize-space() eq '']"/>
    
</xsl:stylesheet>