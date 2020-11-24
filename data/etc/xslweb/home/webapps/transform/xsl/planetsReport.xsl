<?xml version="1.0" encoding="utf-8" ?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="xml" indent="yes" encoding="UTF-8" />
  <xsl:template match="/" mode="start">
    <div class="card m">
      <div class="card-body">
        <h5 class="card-title">Droid samenvatting</h5>
        <p class="card-text">
          <xsl:value-of select="Title"/>
        </p>
        <xsl:apply-templates/>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="Report">
        <p class="card-text">
          <xsl:value-of select="Title"/>
        </p>
        <xsl:for-each select="Profiles/Profile">
          <xsl:variable name="ProfileId" select="@Id"></xsl:variable>
          <p class="card-text">
            Start datum profiling: <xsl:value-of select="StartDate"/>
          </p>
          <p class="card-text">
            Eind datum profiling: <xsl:value-of select="EndDate"/>
          </p>
          <p class="card-text">
            Profiling gemaakt op : <xsl:value-of select="CreatedDate"/>
          </p>
          <p class="card-text">
            Aantal leesbare bestanden: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='File count and sizes']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Count"/>
          </p>
          <p class="card-text">
            Aantal niet-leesbare bestanden: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='Unreadable files']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Count"/>
          </p>
          <p class="card-text">
            Aantal niet-leeasbare mappen: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='Unreadable folders']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Count"/>
          </p>
          <p class="card-text">
            Totale bestandsgrootte: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='File count and sizes']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Sum"/>
          </p>
          <p class="card-text">
            Kleinste bestandsgrootte: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='File count and sizes']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Min"/>
          </p>
          <p class="card-text">
            Grootste bestandsgrootte: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='File count and sizes']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Max"/>
          </p>
          <p class="card-text">
            Gemiddelde bestandsgrootte: <xsl:value-of select="../../ReportItems/ReportItem[Specification/Description='File count and sizes']/Groups/Group/ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Average"/>
          </p>
          <p class="card-text">
            <b>Profiling heeft de volgende mappen verwerkt:</b>
            <hr />
            <xsl:for-each select="//Path">              
              <xsl:value-of select="."/>
              <br />              
            </xsl:for-each>
            <br />
          </p>          
          <p class="card-text">
            <b>Resultaat per jaar</b>
            <br />
            <xsl:for-each select="../../ReportItems/ReportItem[Specification/Description='File count and sizes per year last modified']/Groups/Group[ProfileSummaries/ProfileSummary[@Id=$ProfileId]]">
              <hr />
              Jaar:  <xsl:value-of select="Values/Value"/>
              <br />
              Aantal bestanden: <xsl:value-of select="ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Count"/>
              <br />
              Totale bestandsgrootte: <xsl:value-of select="ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Sum"/>
              <br />
            </xsl:for-each>
            <br />
          </p>
          <p class="card-text">
            <b>Resultaat per formaat</b>
            <br />
              <xsl:for-each select="../../ReportItems/ReportItem[Specification/Description='File sizes per PUID']/Groups/Group[ProfileSummaries/ProfileSummary[@Id=$ProfileId]]">
                <hr />
                PUID: <xsl:value-of select="Values/Value[position()=1]"/>
                <br />
                  Mime: <xsl:value-of select="Values/Value[position()=4]"/>
                <br />
                  Formaat: <xsl:value-of select="Values/Value[position()=2]"/>
                <br />
                  Versie: <xsl:value-of select="Values/Value[position()=3]"/>
                <br />
                  Aantal bestanden: <xsl:value-of select="ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Count"/>
                <br />
                 Totale bestandsgrootte:  <xsl:value-of select="ProfileSummaries/ProfileSummary[@Id=$ProfileId]/Sum"/>
                <br /><br />
              </xsl:for-each>
            <br />
           </p>         
        </xsl:for-each>
  </xsl:template>

</xsl:transform>