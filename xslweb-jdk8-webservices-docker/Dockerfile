# Temporary (but cached) build image
FROM busybox AS build

# During development, XSLWeb was built manually using the latest code from master. So, given the time frame we need
# version 4.0.0-RC2 or better; using the last official release 3.0.1 makes Saxon throw an error for sipcreator.xslt
# though https://armatiek.github.io/xslweb/XSLWeb%20Developer%20Manual.html#external-functions defines 6 arguments:
# XPST0017: Function exec-external must have no more than 5 arguments
ENV XSLWEB_VERSION=4.0.0-RC3

# We could use:
#   ADD https://github.com/Armatiek/xslweb/releases/download/v${XSLWEBVERSION}/xslweb-war-v${XSLWEBVERSION}.zip temp.zip
#   RUN unzip temp.zip xslweb.war
# ...but using wget and piping to unzip in a single RUN command prevents creation of unneeded layers; see also
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#add-or-copy
#
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

# To take advantage of caching, copy any (changed) source code as late as possible
COPY ./home "$XSLWEB_INSTALL_DIR/home"

ENV CATALINA_OPTS "-Dxslweb.home=$XSLWEB_INSTALL_DIR/home"

CMD ["catalina.sh", "run"]
