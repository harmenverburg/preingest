<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:req="http://www.armatiek.com/xslweb/request"
    xmlns:map="http://www.w3.org/2005/xpath-functions/map"
    xmlns:array="http://www.w3.org/2005/xpath-functions/array"
    xmlns:nha="http://noord-hollandsarchief.nl/namespaces/1.0"
    expand-text="yes"
    version="3.0">
    
    <xsl:output method="html" version="5"/>
    
    <xsl:include href="commoncode.xslt"/>
    
    <xsl:template match="/req:request">
        <html xmlns="http://www.w3.org/1999/xhtml">
            <head>
                <title>Archiefbewerkingen</title>
                <link rel="stylesheet" type="text/css" href="{$context-path}/css/gui.css" />
            </head>
            <body>
                <h1>Archiefbewerkingen</h1>
                <h2>Uitgepakte bestanden controleren en bewerken</h2>
                <table>
                    <tbody>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/virusscan/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button
                                >Viruscontrole&#x2026;</button></td>
                        </tr>
                        <tr style="display: none;">
                            <td colspan="3">
                                <div class="report">
                                    <p style="font-weight: bold">Deze tekst is standaard verborgen maar
                                        wordt zichtbaar zodra de bijbehorende actie voltooid is.</p>
                                    <p>Nemo enim ipsam voluptatem, quia voluptas sit, aspernatur aut odit
                                        aut fugit, sed quia consequuntur magni dolores eos, qui ratione
                                        voluptatem sequi nesciunt, neque porro quisquam est, qui dolorem
                                        ipsum, quia dolor sit, amet, consectetur, adipisci velit, sed quia
                                        non numquam eius modi tempora incidunt, ut labore et dolore magnam
                                        aliquam quaerat voluptatem. ut enim ad minima veniam, quis nostrum
                                        exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid
                                        ex ea commodi consequatur?</p>
                                </div>
                            </td>
                            <td></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/naming/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>Bestandsnamen
                                controleren&#x2026;</button></td>
                        </tr>
                        <tr style="display: none">
                            <td colspan="3"><div class="report">
                                <p style="font-weight: bold">Deze tekst is ook standaard verborgen; meer
                                    voorbeelden nemen we echter niet op.</p>
                                <p>Je zou ook kunnen overwegen om kleuren te gebruiken om te laten zien
                                    of iets goed of fout gegaan is (rood/groen).</p>
                            </div></td>
                            <td></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/sidecar/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>Sidecarstructuur controleren&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/profiling/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>Droid voorbereiden&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" /></td>
                            <th>{nha:get-swagger-description('/exporting/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>CSV-bestand aanmaken&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" /></td>
                            <th>{nha:get-swagger-description('/reporting/')}</th>
                            <td>
                                <input class="guid" type="text" size="40"
                                    placeholder="Voer hier de uniekefoldernaam in" /><br />
                                <select>
                                    <option value="">Maak een keuze</option>
                                    <option value="pdf">PDF</option>
                                    <option value="droid">Droid-XML</option>
                                    <option value="planets">Planets-XML</option>
                                </select>
                            </td>
                            <td><button>PDF- of XML-bestand aanmaken&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/greenlist/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>Greenlistcontrole&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/encoding/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>Encodingcontrole&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/validate/')}</th>
                            <td><input class="guid" type="text" size="40"
                                placeholder="Voer hier de uniekefoldernaam in" /></td>
                            <td><button>Schema(tron)-controle&#x2026;</button></td>
                        </tr>
                        <tr style="display: none">
                            <td colspan="3"><div class="report">
                                <p style="font-weight: bold">De tekst zou een rapportage kunnen bevatten van alle schema- en schematron-errors, of een link naar een aparte pagina met die fouten.</p>
                            </div></td>
                            <td></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>{nha:get-swagger-description('/transform/')}</th>
                            <td>
                                <input class="guid" type="text" size="40"
                                    placeholder="Voer hier de uniekefoldernaam in" /><br />
                                <input type="text" size="40"
                                    placeholder="Optioneel: preservica-id voor toevoeging" value="" />
                            </td>
                            <td><button>Omvormen naar XIP&#x2026;</button></td>
                        </tr>
                        <tr>
                            <td><input type="checkbox" checked="checked" /></td>
                            <th>TODO Bestanden laten verwerken door Preservica SIP Creator</th>
                            <td>
                                <input class="guid" type="text" size="40"
                                    placeholder="Voer hier de uniekefoldernaam in" /><br />
                            </td>
                            <td><button>Sipcreator&#x2026;</button></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>