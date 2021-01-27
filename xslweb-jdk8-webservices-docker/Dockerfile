FROM tomcat:jdk8-openjdk

EXPOSE 8080
RUN chmod +x /usr/local/tomcat/bin/catalina.sh

ENV XSLWEBWAR=xslweb.war
ENV EXTERNAL_DATA_EXT_DIR /data/etc
ENV TOMCAT_INSTALL_DIR /usr/local/tomcat
ENV XSLWEB_INSTALL_DIR "/nha/xslweb"
ENV SIPCREATOR_DIR "/nha/SIP_Creator"
ENV TOMCAT_LOG_DIR "/nha/tomcat-logs"

RUN mkdir -p "$XSLWEB_INSTALL_DIR/home" "$SIPCREATOR_DIR" "$TOMCAT_LOG_DIR"
COPY "$XSLWEBWAR" "$TOMCAT_INSTALL_DIR/webapps/ROOT.war"
COPY xslweb/home "$XSLWEB_INSTALL_DIR/home"
COPY zip /usr/bin/

RUN rm -Rf "$TOMCAT_INSTALL_DIR/logs"
RUN ln -s "$TOMCAT_LOG_DIR" "$TOMCAT_INSTALL_DIR/logs" 

# XSLWeb vereist dat de configfile in het classpath staat. Dat doe je door de instelling in een (nog niet bestaand) setenv.sh-script op te nemen.
RUN echo CLASSPATH="\$CLASSPATH:$XSLWEB_INSTALL_DIR/config" >"$TOMCAT_INSTALL_DIR/bin/setenv.sh"
RUN chmod ugo+r "$TOMCAT_INSTALL_DIR/bin/setenv.sh"

ENV CATALINA_OPTS "-Dxslweb.home=$XSLWEB_INSTALL_DIR/home"

CMD ["catalina.sh", "run"]
