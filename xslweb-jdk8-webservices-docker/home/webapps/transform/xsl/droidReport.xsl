<?xml version="1.0" encoding="utf-8" ?>
<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="xml" indent="yes" encoding="UTF-8" />
  <xsl:param name="reportDir" select="'report'"/>
  <xsl:template match="Report">
    <div class="card m">
      <div class="card-body">
        <h5 class="card-title">Comprehensive breakdown</h5>
        <p class="card-text"><xsl:value-of select="Title"/></p>
        <xsl:apply-templates/>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="Title"/>

  <!-- Profile metadata -->
  <xsl:template name="profilesTemplate" match="Profiles">
    <h5 class="card-title">Profile Summary</h5>
    <table class="table">
      <tr>
        <th scope="col">Name</th>
        <th scope="col">Signature version</th>
        <th scope="col">Container version</th>
        <th scope="col">Started</th>
        <th scope="col">Finished</th>
        <th scope="col">Filters</th>
      </tr>
      <xsl:for-each select="Profile">
        <tr>
          <td><xsl:value-of select="Name"/></td>
          <td><xsl:value-of select="SignatureFileVersion"/></td>
          <td><xsl:value-of select="ContainerSignatureFileVersion"/></td>
          <td><xsl:value-of select="StartDate"/></td>
          <td><xsl:value-of select="EndDate"/></td>
          <td>
            <xsl:if test="Filter/Enabled = 'true'">
              <table class="embedded">
                <xsl:if test="count(Filter/Criteria) > 1">
                  <tr>
                    <td colspan="3">
                      <xsl:if test="Filter/Narrowed = 'true'">
                        <xsl:text>(all filter criteria below must be true)</xsl:text>
                      </xsl:if>
                      <xsl:if test="Filter/Narrowed != 'true'">
                        <xsl:text>(any filter criteria below must be true)</xsl:text>
                      </xsl:if>
                    </td>
                  </tr>
                </xsl:if>
                <xsl:for-each select="Filter/Criteria">
                  <tr>
                    <td><xsl:value-of select="FieldName"/><xsl:text> </xsl:text></td>
                    <td><xsl:value-of select="Operator"/><xsl:text> </xsl:text></td>
                    <td><xsl:value-of select="Value"/></td>
                  </tr>
                </xsl:for-each>
              </table>
            </xsl:if>
          </td>
        </tr>
      </xsl:for-each>
    </table>
  </xsl:template>

  <!-- Report items -->
  <xsl:template name="reportItemTemplate" match="ReportItems/ReportItem">
    <h5 class="card-title"><xsl:value-of select="Specification/Description"/></h5>

    <!--  Report item descriptive metadata -->
    <table class="table">
      <tr>
        <th scope="col">Report field</th>
        <th scope="col">Grouping fields</th>
      </tr>
      <tr>
        <td><xsl:value-of select="Specification/Field"/></td>
        <td>
          <xsl:if test="Specification/GroupByFields != ''">
            <table class="table">
              <tr>
                <xsl:for-each select="Specification/GroupByFields/GroupByField">
                  <td>
                    <xsl:if test="Function != ''">
                      <xsl:value-of select="Function"/>
                      <xsl:text>(</xsl:text>
                    </xsl:if>
                    <xsl:value-of select="Field"/>
                    <xsl:if test="Function != ''">
                      <xsl:text>)</xsl:text>
                    </xsl:if>
                  </td>
                </xsl:for-each>
              </tr>
            </table>
          </xsl:if>
        </td>
      </tr>
      <xsl:if test="Specification/Filter != ''">
        <tr>
          <th scope="col" colspan="2">
            <xsl:text>Filter fields: </xsl:text>
            <xsl:if test="count(Specification/Filter/Criteria) > 1">
              <xsl:if test="Specification/Filter/Narrowed = 'true'">
                <xsl:text>(all filter criteria below must be true)</xsl:text>
              </xsl:if>
              <xsl:if test="Specification/Filter/Narrowed != 'true'">
                <xsl:text>(any filter criteria below must be true)</xsl:text>
              </xsl:if>
            </xsl:if>
          </th>
        </tr>
        <tr>
          <th colspan="2">
            <table class="table">
              <tr>
                <th scope="col">Field</th>
                <th scope="col">Operator</th>
                <th scope="col">Values</th>
              </tr>
              <xsl:for-each select="Specification/Filter/Criteria">
                <tr>
                  <td>
                    <xsl:value-of select="FieldName"/>
                    <xsl:text> </xsl:text>
                  </td>
                  <td>
                    <xsl:value-of select="Operator"/>
                    <xsl:text> </xsl:text>
                  </td>
                  <td>
                    <xsl:value-of select="Value"/>
                  </td>
                </tr>
              </xsl:for-each>
            </table>
          </th>
        </tr>
      </xsl:if>
    </table>
    <p/>

    <!-- Report values -->
    <xsl:for-each select="Groups/Group">
      <table class="table">
        <xsl:if test="../../Specification/GroupByFields != ''">
          <tr>
            <th colspan="6" scope="col">
              <table>
                <tr>
                  <xsl:for-each select="Values/Value">
                    <td class="groupvalues">
                      <xsl:value-of select="."/>
                    </td>
                  </xsl:for-each>
                </tr>
              </table>
            </th>
          </tr>
        </xsl:if>
        <tr>
          <th scope="col">Profile</th>
          <th scope="col">Count</th>
          <th scope="col">Sum</th>
          <th scope="col">Min</th>
          <th scope="col">Max</th>
          <th scope="col">Average</th>
        </tr>
        <xsl:for-each select="ProfileSummaries/ProfileSummary">
          <tr>
            <td>
              <xsl:value-of select="Name"/>
            </td>
            <td>
              <xsl:value-of select="Count"/>
            </td>
            <td>
              <xsl:value-of select="Sum"/>
            </td>
            <td>
              <xsl:value-of select="Min"/>
            </td>
            <td>
              <xsl:value-of select="Max"/>
            </td>
            <td>
              <xsl:value-of select="Average"/>
            </td>
          </tr>
        </xsl:for-each>
        <tr>
          <td>Profile totals</td>
          <td>
            <xsl:value-of select="GroupAggregateSummary/Count"/>
          </td>
          <td>
            <xsl:value-of select="GroupAggregateSummary/Sum"/>
          </td>
          <td>
            <xsl:value-of select="GroupAggregateSummary/Min"/>
          </td>
          <td>
            <xsl:value-of select="GroupAggregateSummary/Max"/>
          </td>
          <td>
            <xsl:value-of select="GroupAggregateSummary/Average"/>
          </td>
        </tr>
      </table>
      <p/>
    </xsl:for-each>
    <xsl:if test="Specification/GroupByFields != ''">
      <h3>Group totals</h3>
      <table class="table">
        <th scope="col">Count</th>
        <th scope="col">Sum</th>
        <th scope="col">Min</th>
        <th scope="col">Max</th>
        <th scope="col">Average</th>
        <tr>
          <td>
            <xsl:value-of select="ReportItemAggregateSummary/Count"/>
          </td>
          <td>
            <xsl:value-of select="ReportItemAggregateSummary/Sum"/>
          </td>
          <td>
            <xsl:value-of select="ReportItemAggregateSummary/Min"/>
          </td>
          <td>
            <xsl:value-of select="ReportItemAggregateSummary/Max"/>
          </td>
          <td>
            <xsl:value-of select="ReportItemAggregateSummary/Average"/>
          </td>
        </tr>
      </table>
      <p/>
    </xsl:if>
    <table>
    </table>
  </xsl:template>

</xsl:transform>
