# Temporary (but cached) build image
FROM busybox AS build

ENV XSLWEB_VERSION=4.0.0-RC3

# The WAR distribution is not just the WAR. Instead, for 3.0.1 it holds ./xslweb-3.0.1/xslweb.war and much more, and
# 4.0.0-RC3 comes without a root folder, so holds ./xslweb.war and much more. Only extract the WAR:
RUN wget -qO- \
    https://github.com/Armatiek/xslweb/releases/download/v${XSLWEB_VERSION}/xslweb-war-v${XSLWEB_VERSION}.zip \
    | unzip - xslweb.war

# Final target image
FROM tomcat:jdk8-openjdk

RUN apt-get update && apt-get install -y \
    zip

EXPOSE 8080
RUN chmod +x /usr/local/tomcat/bin/catalina.sh

ENV TOMCAT_INSTALL_DIR /usr/local/tomcat
ENV XSLWEB_INSTALL_DIR "/nha/xslweb"
ENV SIPCREATOR_DIR "/nha/SIP_Creator"
ENV TOMCAT_LOG_DIR "/nha/tomcat-logs"

RUN mkdir -p "$XSLWEB_INSTALL_DIR/home" "$SIPCREATOR_DIR" "$TOMCAT_LOG_DIR"
RUN rm -Rf "$TOMCAT_INSTALL_DIR/logs"
RUN ln -s "$TOMCAT_LOG_DIR" "$TOMCAT_INSTALL_DIR/logs" 

# XSLWeb requires the configuration file to be in the classpath; create a new Tomcat setenv.sh to configure that
RUN echo CLASSPATH="\$CLASSPATH:$XSLWEB_INSTALL_DIR/config" >"$TOMCAT_INSTALL_DIR/bin/setenv.sh"
RUN chmod ugo+r "$TOMCAT_INSTALL_DIR/bin/setenv.sh"

COPY --from=build xslweb.war "$TOMCAT_INSTALL_DIR/webapps/ROOT.war"

# To take advantage of caching, copy any (changed) source code as late as possible (the contents from ./home are
# partially just the defaults from XSLWeb that have been copied into the project)
COPY ./home "$XSLWEB_INSTALL_DIR/home"

ENV CATALINA_OPTS "-Dxslweb.home=$XSLWEB_INSTALL_DIR/home"

CMD ["catalina.sh", "run"]
