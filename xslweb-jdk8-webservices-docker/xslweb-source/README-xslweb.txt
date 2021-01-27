Het ontwikkelen/testen van de XSLWeb-applicaties doe je in de folder nhapreingest/nha/xslweb/home/webapps/.

Het docker-image bevat een kopie van deze folder (dus van nha/xslweb).

Als je niet het docker-image met XSLWeb gebruikt, moet je XSLWeb zelf installeren. Ik doe dat door het zelf te downloaden en te compileren. Dat gaat zo:

git clone git@github.com:Armatiek/xslweb.git

Daarna build je xslweb met "mvn clean install"

Om XSLWeb vanuit Tomcat te draaien, maak je een kopie van de *.war-file in de target-folder (gemaakt door Maven)
en plaats je die als ROOT.war in de folder webapps van Tomcat (bestaande ROOT.war verwijderen). Of je legt een soft link aan.

XSLWeb vereist een extra configuratie binnen Tomdat: maak in de Tomcat-folder een file bin/setenv.sh (of setenv.bat)
aan met bijvoorbeeld als inhoud:

CLASSPATH=$CLASSPATH:/pad/naar/xslweb/home/config
CATALINA_OPTS=CATALINA_OPTS="-Dxslweb.home=/pad/naar/xslweb/home"

Voor setenv.bat gebruik je natuurlijk % % in plaats van een $.

Tenslotte plaats je, nog steeds bij het ontwikkelen, in de folder xslweb/home/webapps soft links naar de webapplicatiefolders in nhapreingest/etc/xslweb/home/webapps/.
Of je maakt een kopie van die folders.