FROM tomcat:jdk8-openjdk

EXPOSE 8080
RUN chmod +x /usr/local/tomcat/bin/catalina.sh

ENV XSLWEBWAR=xslweb.war
ENV EXTERNAL_DATA_EXT_DIR /data/etc
ENV TOMCAT_INSTALL_DIR /usr/local/tomcat
ENV XSLWEB_INSTALL_DIR "$EXTERNAL_DATA_EXT_DIR/xslweb"

COPY "$XSLWEBWAR" "$TOMCAT_INSTALL_DIR/webapps/ROOT.war"
COPY zip /usr/bin/

RUN rm -Rf "$TOMCAT_INSTALL_DIR/logs"
RUN ln -s "$EXTERNAL_DATA_EXT_DIR/tomcat/logs" "$TOMCAT_INSTALL_DIR/logs" 

# XSLWeb vereist dat de configfile in het classpath staat. Dat doe je door de instelling in een (nog niet betstaand) setenv.sh-script op te nemen.
RUN echo CLASSPATH="\$CLASSPATH:$XSLWEB_INSTALL_DIR/config" >"$TOMCAT_INSTALL_DIR/bin/setenv.sh"
RUN chmod ugo+r "$TOMCAT_INSTALL_DIR/bin/setenv.sh"

ENV CATALINA_OPTS "-Dxslweb.home=$XSLWEB_INSTALL_DIR/home"

CMD ["catalina.sh", "run"]
