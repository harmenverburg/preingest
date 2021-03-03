# Noord-Hollands Archief pre-ingest tooling

A collection of tools for validation and transformation, to support transferring TMLO/ToPX archives to
[the NHA Preservica e-Depot](https://noord-hollandsarchief.nl/informatiebeheer/e-depot).

:warning: THIS IS VERY MUCH WORK IN PROGRESS. For details, [contact us](mailto:arjan.van.bentem@noord-hollandsarchief.nl).

This expects `.tar` or `.tar.gz` archives confirming to the Dutch National Archives TMLO/ToPX sidecar format. Once put
into the configured input folder, the API [or frontend](https://github.com/noord-hollandsarchief/preingest-frontend) can
be used to configure the file details (such as the expected checksum and details about the target location), after which
validations and transformations can be scheduled. When all fine, Preservica's SIP Creator will be invoked to create a
XIP v4 SIP package, which will be moved to Preservica's Transfer Agent's upload folder. 


## Available actions

The following validation and transformation actions are currently implemented:

- Calculate and validate the archive's MD-5, SHA-1, SHA-256 or SHA-512 checksum.
- Extract the `.tar` or `.tar.gz` archive.
- Check for viruses. This [uses ClamAV](https://hub.docker.com/r/mkodockx/docker-clamav) and updates its definitions
  automatically. It also scans compressed archives, OLE2 documents and email files.
- [Validate the file names](preingest/Noord.HollandsArchief.Pre.Ingest.WebApi/Handlers/NamingValidationHandler.cs), to
  not contain invalid characters, and not be Windows/DOS-reserved names such as `PRN` or `AUX`.
- [Validate the TMLO/ToPX sidecar structure](preingest/Noord.HollandsArchief.Pre.Ingest.WebApi/Handlers/SidecarValidationHandler.cs).
- Classify the file types, to determine PRONOM IDs and to see if the extension matches the actual content. This 
  [uses DROID](droid-validation/README.md) and does NOT currently automatically update its file database.
- Export the DROID results to CSV, XML or PDF. The results are also included in the final Excel report.
- [Compare the files types](preingest/Noord.HollandsArchief.Pre.Ingest.WebApi/Handlers/GreenListHandler.cs) against
  the [list of preferred formats](preingest/Noord.HollandsArchief.Pre.Ingest.WebApi/Datasource/greenlist.json).
- [Validate the file encoding](preingest/Noord.HollandsArchief.Pre.Ingest.WebApi/Handlers/EncodingHandler.cs) to always
  be UTF8.
- [Validate the sidecar files](xslweb-jdk8-webservices-docker/home/webapps/topxvalidation/xsl/request-dispatcher.xsl)
  against the ToPX XSD and [Schematron rules](xslweb-jdk8-webservices-docker/home/webapps/topxvalidation/sch/topx.sch).
  Some of the Schematron rules may be specific to the requirements of the Noord-Hollands Archief.
- [Transform the ToPX metadata](xslweb-jdk8-webservices-docker/home/webapps/transform/xsl/topx2xip.xslt) files to
  Preservica XIP v4, also mapping ToPX `<omschrijvingBeperkingen>` to Preservica security tags, specific to the
  requirements of the Noord-Hollands Archief.
- Run SIP Creator to transform XIP v4 to SIP.
- [Validate the SIP Creator result](xslweb-jdk8-webservices-docker/home/webapps/xipvalidation/xsl/request-dispatcher.xsl)
  against `XIP-V4.xsd`.
- [Copy the SIP Creator result](preingest/Noord.HollandsArchief.Pre.Ingest.WebApi/Handlers/SipZipCopyHandler.cs) to the
  Transfer Agent.
- [Create an overall Excel report](xslweb-jdk8-webservices-docker/home/webapps/excelreport/xsl/request-dispatcher.xsl).

Whenever a validation fails, its result is set to Error, regardless the severity. If any error occurred in the currently
scheduled actions, then the action to copy the SIP to the Transfer Agent area will not be started but marked as Failed.
This can be ignored by re-scheduling that very action again.

Multiple files can be handled in parallel, but for each file the API simply executes any requested action sequentially.
The frontend will enforce a specific order and take care of scheduling dependencies. The frontend will also allow some
actions to be re-scheduled any time, but most actions cannot be run again once they succeeded.

## Overall status

Overall result while processing an archive (collection):

- `Running` if any action is currently running, else:
- `Failed` if any of the actions reported a fatal error, else:
- `Error` if any of the actions reported an error, else:
- `Success` if any action was executed and all returned Success, else:
- `New` if nothing happened for an archive yet.

As saving the settings is basically an action too, doing so will change the overall state from `New` to `Success`.

## Requirements

- A recent version of Docker. Given [the large number](https://github.com/mko-x/docker-clamav#memory) of virus signature
  definitions, this may need at least 3 GB of memory.

- Preservica [SIP Creator 5.11](https://usergroup.preservica.com/downloads/view.php?resource=SIP+Creator&version=5.11).
  Its command line interface will be invoked using Java 8 from a Linux Docker container. This has been tested using the
  Linux 64 bit version ZIP download, but Windows installations may work on Linux too. A bug in the SIP Creator CLI
  requires installation in a folder without spaces. Beware that Client Installer 5.10 and 5.11 do not include the SIP
  Creator command line interface. Also note that the original Linux `createsip` script is not used; instead
  [a custom version](xslweb-jdk8-webservices-docker/home/webapps/sipcreator/scripts/nha-createsip) is included in the
  Docker image. And beware that 5.10 [does not suffice](https://eu.preservica.com/sdb/help/documentation):
  
  > #### 7.6. XIP Metadata Fragments
  > 
  > As of version 5.11, users can use the bulk metadata option to specify and override XIP fields as well as just
  > attaching generic metadata. To do this, the .metadata file should contain XML in the XIP format and namespace. When
  > using this with XIP File fragments, this mechanism can also be used to verify pre-existing checksums/fixity values
  > at the point of SIP creation.

  This information (such as `<Title>` and `<SecurityTag>`) is silently discarded when using SIP Creator 5.10. 

- Preservica Transfer Agent, configured and ready to use. This has been tested using [Preservica Client Installer
  5.10](https://usergroup.preservica.com/downloads/view.php?resource=Client+Installer&version=5.10). Version 5.11
  may work too, but in February 2021 that included an incomplete Java runtime. Though this a Java application, it is
  only supported on Windows, and only for Cloud Edition, not for on-premise installations.
  
  To support both test and production environments, this needs to be running twice:
  
  - Install it using the Client Installer 5.10 wizard. You'll need a URL like `https://eu.preservica.com/sdb` and can
    likely ignore a warning saying _"Connection to Preservica has been re-directed. Please check the URL and re-try.
    This often happens if http is used instead of https"_.
    
  - After installing it once, duplicate the result folder in `C:\Preservica\Local\Transfer Agent` to, say,
    `C:\Preservica\Local\Transfer Agent Production`.
    
  - In the copy, adjust `installService.bat` to use different names for:
  
    ```text
    set SERVICE_NAME=TransferAgent
    set PR_DISPLAYNAME=Preservica Transfer Agent
    ```
  
  - In the copy, adjust the configuration file `C:\Preservica\Local\Transfer Agent Production\conf\upload.properties`
    and create a dedicated upload folder:

    ```text
    upload.root=C:/Preservica/Local/UploadArea-Production
   
    preservica.username=prod-user@example.com
    preservica.password=prod-password
    preservica.url=https://eu.preservica.com/sdb
    ```

  - Aside, [note](https://usergroup.preservica.com/documentation/ce/6.2.1/html/SIPCreatorSUG.html):

    > The Transfer Agent upload area must be hosted on the same system as the Transfer Agent application. The amount of
    > free disk space required by the Transfer Agent depends on the maximum size of packages being uploaded at the same
    > time. At least three times the maximum size simultaneous uploads is recommended.
    >
    > Installation of the Preservica client applications under "Program Files" or "Program Files (x86)" is not
    > recommended due to access restrictions in place on these folders. If the client applications are installed in one
    > of these folders then all standard permissions (Modify, Read & execute, List folder contents, Read and Write) must
    > be granted to the Windows group "Authenticated Users"

- The [frontend web application](https://github.com/noord-hollandsarchief/preingest-frontend). Alternatively, without
  building anything yourself, you can use the included [docker-compose](docker-compose.yml) like described below.

Some intermediate results are stored in a SQLite database. This database will be rebuilt automatically when starting
afresh. To avoid database errors you may want to exclude its working folder from any virus scanning.


## Docker

Some Docker images expect folders like `/data`, `/nha/SIP_Creator` and `/nha/tomcat-logs` to be mapped to some folder
on the Docker host.

:warning: Docker does not support symbolic links (unless they're supposed to map to folders inside the containers).

- Make the settings known in environment variables. For example:
  
  - Create a `.env` file in the root of this project, holding lines like:
  
    ```text
    DATAFOLDER=/path/to/data
    SIPCREATORFOLDER=/path/to/sip-creator
    TOMCATLOGFOLDER=/path/to/tomcat-logs
    TRANSFERAGENTTESTFOLDER=/path/to/transfer-agent-test-folder
    TRANSFERAGENTPRODFOLDER=/path/to/transfer-agent-production-folder
    ```

  - Or, `export` the values in your Linux environment:
    
    ```text
    export DATAFOLDER=/path/to/data
    export SIPCREATORFOLDER=/path/to/sip-creator
    export TOMCATLOGFOLDER=/path/to/tomcat-logs
    export TRANSFERAGENTTESTFOLDER=/path/to/transfer-agent-test-folder
    export TRANSFERAGENTPRODFOLDER=/path/to/transfer-agent-production-folder
    ```

  - Or, `set` the values in your Windows environment:
    
    ```text
    set DATAFOLDER=D:\path\to\data
    set SIPCREATORFOLDER=D:\path\to\sip-creator
    set TOMCATLOGFOLDER=D:\path\to\tomcat-logs
    set TRANSFERAGENTTESTFOLDER=D:\path\to\transfer-agent-test-folder
    set TRANSFERAGENTPRODFOLDER=D:\path\to\transfer-agent-production-folder
    ```

- To run all Docker containers, pulling the development images if needed, run:

  ```text
  docker-compose up
  ```

- To build new images locally, adjust [docker-compose.dev.yml](docker-compose.dev.yml) as needed, and use with:

  ```text
  docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
  ```

  This expects [the frontend code](https://github.com/noord-hollandsarchief/preingest-frontend) to have been cloned into
  `../preingest-frontend`.

  Alternatively, copy that example `docker-compose.dev.yml` into a file `docker-compose.override.yml`, which will be
  used automatically when `docker-compose up` is used.
  
  When images already exist (like after running `docker-compose pull` or when built earlier), add `--build` to force a
  new build.

### Preingest database location 

Database file will be stored (default) in `/data/preingest.db`. Location can be changed by supplying a `environment` parameter in de docker-compose.yml under the preingest service. 
Example:

```yaml
environment:
   - "ConnectionStrings:Sqlite=Data Source=/{folder}/{name}"
```

## Development

The API, scheduler and non-XML validations use Microsoft .NET Core, while XML validations and transformations use
[XSLWeb pipelines](https://github.com/Armatiek/xslweb). As Microsoft Excel files are basically ZIP archives with XML
files, the Excel report is created using an XML transformation.

All Dockerfiles ensure one can build any part without the need to install any specific development environment.

### OpenAPI (formerly Swagger)

When using `docker-compose.yml`, the internal API can be accessed from `localhost:8000`, like
<http://localhost:8000/api/preingest/check> to see the API's health status. An OpenAPI specification is available at
<http://localhost:8000/swagger/v1/swagger.json>, and a built-in Swagger UI at <http://localhost:8000/swagger>.

### SignalR/WebSocket

Three script samples are available for receiving status update/events:
- http://localhost:8000/events.html
- http://localhost:8000/collections.html
- http://localhost:8000/collection.html

## Troubleshooting

- `clamav exited with code 137` and `/bootstrap.sh: line 35: 15 Killed clamd`: increase the memory for the Docker host.

- `Preparing SIP Creator library for first use` followed by `ls: cannot access 'plugins/com.tessella.sdb.core.xip.gui_*.jar':
  No such file or directory`: ensure there are no spaces in the path of `SIPCREATORFOLDER`.

- Transfer Agent Windows service fails to start, complaining that the JRE cannot be instantiated: make sure to use
  Client Installer 5.10, or provide a working Java runtime yourself when using 5.11.

- `SQLite Error 14: 'unable to open database file'`: exclude the database file (default: `DATAFOLDER/preingest.db`)
  from virus scanning.

## Trademarks

Preservica™ is a trademark of [Preservica Ltd](https://preservica.com/). The Noord-Hollands Archief is not affiliated
with that organisation. 
