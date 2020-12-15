# DROID HTTP wrapper

An HTTP API wrapper around the British National Archive's Digital Record Object Identification tool.

As explained [on its website](https://www.nationalarchives.gov.uk/information-management/manage-information/policy-process/digital-continuity/file-profiling-tool-droid/):

> DROID stands for Digital Record Object Identification. It’s a free software tool developed by The National Archives
> that will help you to automatically profile a wide range of file formats. For example, it will tell you what versions
> you have, their age and size, and when they were last changed. It can also provide you with data to help you find
> duplicates.

See also [the DROID GitHub pages](http://digital-preservation.github.io/droid/).

## Docker

The Dockerfile builds the Spring Boot project using a temporary build image, and creates a final Docker image that
also includes the DROID binary.

```text
docker build -t noordhollandsarchief/droidvalidationwebapi:development .
docker push noordhollandsarchief/droidvalidationwebapi:development
```

## Development

On a development machine with Java 11 installed, `./mvnw spring-boot:run` will run the server on `localhost:8080`, for
which <http://localhost:8080/api/droid/v6.5/> (including the trailing slash) will show a status message. Other endpoints
can be found in [DroidController](./src/main/java/nl/noord/hollandsarchief/droid/DroidController.java).
