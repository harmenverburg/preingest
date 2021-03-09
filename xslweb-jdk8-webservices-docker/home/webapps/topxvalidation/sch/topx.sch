<?xml version="1.0" encoding="UTF-8"?>
<schema xmlns="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
    <ns uri="http://www.nationaalarchief.nl/ToPX/v2.3" prefix="topx"/>
    
    <let name="max-naam-length" value="xs:integer(255)"/>
    
    <pattern id="topx-pattern-1">
        <rule context="topx:aggregatie | topx:bestand">
            <p>Controleer of er precies één element "naam" is (dus niet 0 of meer dan 1)</p>
            <report test="count(topx:naam) eq 0">Het "naam"-gegeven ontbreekt in de ToPX-metadata</report>
            <report test="count(topx:naam) gt 1">Er is meer dan één "naam"-gegeven in de ToPX-metadata</report>
            
            <assert test="string-length(topx:naam) le $max-naam-length"
                >De lengte van het "naam"-gegeven (voor de titel) is groter dan <value-of select="$max-naam-length"/> tekens.</assert>
           
            <p>Controleer of er precies één element "omschrijving" is (dus niet 0 of meer dan 1)</p>
            <report test="count(topx:classificatie/topx:omschrijving) eq 0">Het "classificatie/omschrijving"-gegeven  ontbreekt in de ToPX-metadata</report>
            <report test="count(topx:classificatie/topx:omschrijving) gt 1">Er is meer dan één "classificatie/omschrijving"-gegeven in de ToPX-metadata</report>
            
            <p>Controleer of er precies één element "omschrijvingBeperkingen" is (dus niet 0 of meer dan 1)</p>
            <report test="count(topx:openbaarheid/topx:omschrijvingBeperkingen) eq 0">Het "openbaarheid/omschrijvingBeperkingen"-gegeven ontbreekt in de ToPX-metadata</report>
            <report test="count(topx:openbaarheid/topx:omschrijvingBeperkingen) gt 1">Er is meer dan één "openbaarheid/omschrijvingBeperkingen"-gegeven in de ToPX-metadata</report>
        </rule>
    </pattern>
    
    <pattern id="topx-pattern-2">
        <rule context="topx:omschrijvingBeperkingen">
            <p>Controleer de tekstuele inhoud van element "omschrijvingBeperkingen"</p>
            <assert test="matches(., '^(toegang_publiek(_metadata)?|toegang_intern(_\S+)?)$', 'i')"
                >De inhoud van het gegeven "omschrijvingBeperkingen" voldoet niet aan het vereiste patroon</assert>
        </rule>
    </pattern>
    
    <pattern id="topx-pattern-3">
        <rule context="topx:bestand">
            <p>Controleer of er precies één element "algoritme" is (dus niet 0 of meer dan 1)</p>
            <report test="count(topx:formaat/topx:fysiekeIntegriteit/topx:algoritme) eq 0">Het "formaat/fysiekeIntegriteit/algoritme"-gegeven ontbreekt in de ToPX-metadata</report>
            <report test="count(topx:formaat/topx:fysiekeIntegriteit/topx:algoritme) gt 1">Er is meer dan één "formaat/fysiekeIntegriteit/algoritme"-gegeven in de ToPX-metadata</report>
            
            <p>Controleer of er precies één element "waarde" (behorend bij "algoritme") is (dus niet 0 of meer dan 1)</p>
            <report test="count(topx:formaat/topx:fysiekeIntegriteit/topx:waarde) eq 0">Het "formaat/fysiekeIntegriteit/waarde"-gegeven ontbreekt in de ToPX-metadata</report>
            <report test="count(topx:formaat/topx:fysiekeIntegriteit/topx:waarde) gt 1">Er is meer dan één "formaat/fysiekeIntegriteit/waarde"-gegeven in de ToPX-metadata</report>
        </rule>
    </pattern>
    
    <pattern id="topx-pattern-4">
        <rule context="topx:algoritme">
            <p>Controleer de tekstuele inhoud van element "algoritme"</p>
           <assert test="matches(., '^(MD5|SHA1|SHA256|SHA512)$')"
               >De waarde van het "algoritme"-gegeven in de ToPX-metadata is niet MD5, SHA1, SHA256, of SHA512</assert>
        </rule>
    </pattern>
</schema>