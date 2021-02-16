# Noord-Hollands Archief Pre-Ingest tooling

A collection of tools for validation and transformation, to support transferring archives to
[the NHA e-Depot](https://noord-hollandsarchief.nl/informatiebeheer/e-depot).

:warning: THIS IS VERY MUCH WORK IN PROGRESS. For details, [contact us](mailto:arjan.van.bentem@noord-hollandsarchief.nl).


## Development

Some Docker images expect folders like `/data`, `/nha/SIP_Creator` and `/nha/tomcat-logs` to be mapped to some folder
on the Docker host.

:warning: Docker does not support symbolic links (unless they're supposed to map to folders inside the containers).

- Make the location of the incoming archives known in environment variables such as `DATAFOLDER`. For example:
  
  - Create a `.env` file in the root of this project, holding lines like:
  
    ```text
    DATAFOLDER=/path/to/data
    SIPCREATORFOLDER=/path/to/SIP_Creator
    TOMCATLOGFOLDER=/path/to/tomcat-logs
    TRANSFERAGENTTESTFOLDER=/path/to/tatf
    TRANSFERAGENTPRODFOLDER=/path/to/tapf
    ```

  - or:
    
    ```text
    export DATAFOLDER=/path/to/data
    export SIPCREATORFOLDER=/path/to/SIP_Creator
    export TOMCATLOGFOLDER=/path/to/tomcat-logs
    export TRANSFERAGENTTESTFOLDER=/path/to/tatf
    export TRANSFERAGENTPRODFOLDER=/path/to/tapf
    ```

  - or:
    
    ```text
    set DATAFOLDER=D:\path\to\data
    set SIPCREATORFOLDER=D:\path\to\SIP_Creator
    set TOMCATLOGFOLDER=D:\path\to\tomcat-logs
    set TRANSFERAGENTTESTFOLDER=D:\path\to\tatf
    set TRANSFERAGENTPRODFOLDER=D:\path\to\tapf
    ```

- To run all Docker containers, pulling the development images if needed, run:

  ```text
  docker-compose up
  ```

- To build new images locally, see [docker-compose.dev.yml](docker-compose.dev.yml) and use with:

  ```text
  docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
  ```

  Alternatively, copy that example `docker-compose.dev.yml` into a file `docker-compose.override.yml`, which will be
  used automatically when `docker-compose up` is used.

### OpenAPI (formerly Swagger)

When using `docker-compose.yml`, the internal API can be accessed from `localhost:8000`, like
<http://localhost:8000/api/preingest/check> to see the API's health status. An OpenAPI specification is available at
<http://localhost:8000/swagger/v1/swagger.json>, and a built-in Swagger UI at <http://localhost:8000/swagger>.

### Preingest database location 

Database file will be stored (default) in `/data/preingest.db`. Location can be changed by supplying a `environment` parameter in de docker-compose.yml under the preingest service. 
Example:
`environment:`
        `- "ConnectionStrings:Sqlite=Data Source=/{folder}/{name}"`

### SignalR/WebSocket

Three script samples are available for receiving status update/events:
- http://localhost:8000/events.html
- http://localhost:8000/collections.html
- http://localhost:8000/collection.html

## Troubleshooting

- `clamav exited with code 137` and `/bootstrap.sh: line 35: 15 Killed clamd`: increase the memory for the Docker host

- `Preparing SIP Creator library for first use` followed by `ls: cannot access 'plugins/com.tessella.sdb.core.xip.gui_*.jar':
  No such file or directory`: ensure there are no spaces in the path of `SIPCREATORFOLDER`