#maven build: ./mvnw package && java -jar target/droid-0.0.1-SNAPSHOT.jar
#build : docker build -t nhapreingest/noordhollandsarchiefdroidvalidationwebapi:development .
#docker run development : docker run -it -p 8080:8080 -v "D:\data:/data" -v "D:\01_Noord-Hollands Archief\Source\Repos\Droid:/usr/src/droid" nhapreingest/noordhollandsarchiefdroidvalidationwebapi:development
#docker run non-development : docker run -it -p 8080:8080 -v "D:\data:/data" nhapreingest/noordhollandsarchiefdroidvalidationwebapi:development


FROM openjdk:11

WORKDIR /
RUN mkdir droid
COPY bin /droid/

ARG JAR_FILE=*.jar

COPY ${JAR_FILE} app.jar
ENTRYPOINT ["java","-jar","/app.jar"]

EXPOSE 8080
