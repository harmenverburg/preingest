<?xml version="1.0" encoding="UTF-8"?>
<schema xmlns="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
    <ns uri="http://www.nationaalarchief.nl/ToPX/v2.3" prefix="topx"/>
    
    <pattern id="topx-pattern-1">
        <rule context="topx:aggregatie | topx:bestand">
            <p>Controleer of er precies één element "naam" is (dus niet 0 of meer dan 1)</p>
            <assert test="count(topx:naam) eq 1"
                >Het aantal naam-elementen in de ToPX-metadata is niet 1 maar <value-of select="count(topx:naam)"/></assert>
            
            <assert test="string-length(topx:naam) le 255"
                >De lengte van het naam-veld (voor de titel) is te groot: <value-of select="string-length(topx:naam)"/>. De inhoud van het naam-veld is: "<value-of select="topx:naam"/>".</assert>
            
            <p>Controleer of er precies één element "omschrijving" is (dus niet 0 of meer dan 1)</p>
            <assert test="count(topx:classificatie/topx:omschrijving) eq 1"
                >Het aantal classificatie-elementen met een omschrijving-element in de ToPX-metadata is niet 1 maar <value-of select="count(topx:classificatie/topx:omschrijving)"/></assert>
            
            <p>Controleer of er precies één element "omschrijvingBeperkingen" is (dus niet 0 of meer dan 1)</p>
            <assert test="count(topx:openbaarheid/topx:omschrijvingBeperkingen) eq 1"
                >Het aantal openbaarheid-elementen met een omschrijvingBeperkingen-element in de ToPX-metadata is niet 1 maar <value-of select="count(topx:openbaarheid/topx:omschrijvingBeperkingen)"/></assert>
        </rule>
    </pattern>
    
    <pattern id="topx-pattern-2">
        <rule context="topx:omschrijvingBeperkingen">
            <!-- TODO dit moet nog beter worden uitgewerkt. -->
            <p>Controleer de tekstuele inhoud van element "omschrijvingBeperkingen"</p>
            <assert test="matches(., '^(Openbaar|Beperkt openbaar [ABC])$')"
                >De waarden van omschrijvingBeperkingen in de ToPX-metadata is niet Openbaar,of Beperkt openbaar A/B/C maar "<value-of select="."/>"</assert>
        </rule>
    </pattern>
    
    <pattern id="topx-pattern-3">
        <rule context="topx:bestand">
            <p>Controleer of er precies één element "algoritme" is (dus niet 0 of meer dan 1)</p>
            <assert test="count(topx:formaat/topx:fysiekeIntegriteit/topx:algoritme) eq 1"
                >Het aantalformaat/fysiekeIntegriteit-elementen met een algoritme-element in de ToPX-metadata binnen formaat/ is niet 1 maar <value-of select="count(topx:formaat/topx:fysiekeIntegriteit/topx:algoritme)"/></assert>
            
            <p>Controleer of er precies één element "waarde" (behorend bij "algoritme") is (dus niet 0 of meer dan 1)</p>
            <assert test="count(topx:formaat/topx:fysiekeIntegriteit/topx:waarde) eq 1"
                >Het aantalformaat/fysiekeIntegriteit-elementen met een waarde-element in de ToPX-metadata binnen formaat/ is niet 1 maar <value-of select="count(topx:formaat/topx:fysiekeIntegriteit/topx:waarde)"/></assert>
        </rule>
    </pattern>
    
    <pattern id="topx-pattern-4">
        <rule context="topx:algoritme">
            <p>Controleer de tekstuele inhoud van element "algoritme"</p>
           <assert test="matches(., '^(MD5|SHA1|SHA256|SHA512)$')"
               >De waarden van algoritme in de ToPX-metadata is niet MD5, SHA1, SHA256, of SHA512 maar "<value-of select="."/>"</assert>
        </rule>
    </pattern>
</schema>