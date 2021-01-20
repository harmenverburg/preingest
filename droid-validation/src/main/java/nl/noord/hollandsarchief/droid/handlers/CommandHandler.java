package nl.noord.hollandsarchief.droid.handlers;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.file.CopyOption;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.ObjectWriter;

import org.springframework.web.client.RestTemplate;
import nl.noord.hollandsarchief.droid.entities.NewActionResult;
import nl.noord.hollandsarchief.droid.entities.NewResultItem;
import nl.noord.hollandsarchief.droid.entities.RootMessage;
import nl.noord.hollandsarchief.droid.entities.UpdateResult;
import nl.noord.hollandsarchief.droid.entities.BodyMessage;
import nl.noord.hollandsarchief.droid.entities.BodyNotifyEventMessage;
import nl.noord.hollandsarchief.droid.entities.BodyUpdateMessage;
import nl.noord.hollandsarchief.droid.entities.ActionStateOptions;
import nl.noord.hollandsarchief.droid.entities.BodyAction;

public abstract class CommandHandler implements ICommandHandler {
  protected String ARCHIVEDATA_LINUX_FOLDER = "/data/";
  protected String DROID_LINUX_FOLDER = "/droid/";

  private DateTimeFormatter _formatter = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss.SSSSSSS");

  public String currentApplicationLocation() {
    return System.getProperty("user.dir");
  }

  public boolean existsArchiveFolder() {
    boolean result = false;

    Path archive = Paths.get(this.ARCHIVEDATA_LINUX_FOLDER, new String[0]);
    result = Files.exists(archive, new java.nio.file.LinkOption[0]);
    if (!result) {
      System.out.println("Archive folder not found : " + this.ARCHIVEDATA_LINUX_FOLDER);
    }

    return result;
  }

  public boolean existsDroidFolder() {
    boolean result = false;
    Path archive = Paths.get(this.DROID_LINUX_FOLDER, new String[0]);
    result = Files.exists(archive, new java.nio.file.LinkOption[0]);
    if (!result) {
      System.out.println("Archive folder not found : " + this.DROID_LINUX_FOLDER);
    }
    return result;
  }

  protected void runSeperateThread(String handlerName, String[] commandArgs) throws IOException {
    runSeperateThread(handlerName, null, null, commandArgs);
  }

  protected void runSeperateThread(String handlerName, String processGuid, String guid, String[] commandArgs) throws IOException {

    ZonedDateTime start = ZonedDateTime.now(ZoneId.of("UTC"));         
    String startDateTimeNow = start.format(_formatter);
    

    System.out.println(" ");
    System.out.println("==========Arguments Passed From Command line===========");
    for (String args : commandArgs)
      System.out.println(args);
    System.out.println("=======================================================");
    System.out.println(" ");

    if (guid == null || processGuid == null) {     

      final Thread mainThread = new Thread() {
        @Override
        public void run() {
          
            UUID uuid = UUID.randomUUID();
            String randomGuid = uuid.toString();          

          try {
            notifyClient(randomGuid, handlerName, "Start DROID application.", ActionStateOptions.Started, startDateTimeNow); 
            executeLogic(randomGuid, handlerName, commandArgs);
          } catch (final Exception e) {
            e.printStackTrace();
          } finally {
            notifyClient(randomGuid, handlerName, "Done with DROID application.", ActionStateOptions.Completed, startDateTimeNow); 
          }
        }
      };
      mainThread.start();
    } else {

      final Thread mainThread = new Thread() {
        @Override
        public void run() {

          boolean isError = false;
          UpdateResult update = UpdateResult.Error;
          try {
            // send start signal
            if (processGuid != null) {
              String actionGuid = processGuid;
              registerStartStatus(actionGuid);    
              notifyClient(guid, handlerName, "Start DROID application.", ActionStateOptions.Started, startDateTimeNow);            
            }

            update = executeLogic(guid, handlerName, commandArgs);

          } catch (final Exception e) {
            // send error signal
            // return of course
            e.printStackTrace();
            if (processGuid != null) {
              BodyMessage message = new BodyMessage();
              message.message = e.getMessage();
              String actionGuid = processGuid;
              registerFailedStatus(actionGuid, message);

              notifyClient(guid, handlerName, "An exception occured with DROID application.", ActionStateOptions.Failed, startDateTimeNow); 

              update = UpdateResult.Failed;
            }
            isError = true;
          } finally {
            // send completed signal
            if (processGuid != null && !isError) {
              String actionGuid = processGuid;
              registerCompletedStatus(actionGuid);

              notifyClient(guid, handlerName, "Done with DROID application.", ActionStateOptions.Completed, startDateTimeNow); 
            }
          }

          //update the action with result and summary
          ZonedDateTime end = ZonedDateTime.now(ZoneId.of("UTC"));         
          String endDateTimeNow = end.format(_formatter);
          try{
            String actionGuid = processGuid;
            updateNewAction(actionGuid, update, startDateTimeNow, endDateTimeNow);
          }
          catch(final Exception e){
            e.printStackTrace();
          }finally{ }

        }
      };
      mainThread.start();
    }
  }

  protected NewActionResult registerNewAction(String folderGuid, BodyAction jsonData) {

    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = env + "/api/status/new/" + folderGuid;
    } else {

      System.out.println(" ");
      System.out.println("==========Warning===========");
      System.out.println(
          "ENVIRONMENT VARIABLE 'PREINGEST_WEBAPI' is not found. Please add ENVIRONMENT VARIABLE with protocol+servername(or server ip) without forward slash.");
      System.out.println("This action will not be register in the local database.");
      System.out.println("============================");
      System.out.println(" ");
    }
    NewActionResult result = null;
    try {
      RestTemplate restTemplate = new RestTemplate();
      result = restTemplate.postForObject(url, jsonData, NewActionResult.class);

      if (result != null) {
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();
        String json = ow.writeValueAsString(result);
        System.out.println(json);
      }

    } catch (Exception e) {
      e.printStackTrace();
    }

    return result;
  }

  protected void updateNewAction(String actionGuid, UpdateResult update, String start, String end){
    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = env + "/api/status/update/" + actionGuid;
    } else {
      System.out.println(" ");
      System.out.println("==========Warning===========");
      System.out.println(
          "ENVIRONMENT VARIABLE 'PREINGEST_WEBAPI' is not found. Please add ENVIRONMENT VARIABLE with protocol+servername(or server ip) without forward slash.");
      System.out.println("Failed result will not be register in the local database.");
      System.out.println("============================");
      System.out.println(" ");
      return;
    }

    try {
      BodyUpdateMessage message = new BodyUpdateMessage();      
      if(update == UpdateResult.Success)
      {
        message.result = UpdateResult.Success.name();
        message.summary = String.format("{\"processed\": 1,\"accepted\": 1,\"rejected\": 0,\"start\": \"%1$2s\",\"end\": \"%2$2s\"}", new Object[] { start+"+00:00", end+"+00:00"});
      }
  
      if(update == UpdateResult.Error || update == UpdateResult.Failed)
      {
        if(update == UpdateResult.Error)
          message.result =  UpdateResult.Error.name();

        if(update == UpdateResult.Failed);
          message.result =  UpdateResult.Failed.name();

        message.summary = String.format("{\"processed\": 1,\"accepted\": 0,\"rejected\": 1,\"start\": \"%1$2s\",\"end\": \"%2$2s\"}", new Object[] {start+"+00:00", end+"+00:00"});
      }

      RestTemplate restTemplate = new RestTemplate();
      Map<String, String> params = new HashMap<String, String>();
      params.put("actionGuid", actionGuid);
      
      restTemplate.put(url, message, params);
      
      
    } catch (Exception e) {
      e.printStackTrace();
    }
  }

  protected void notifyClient(String folderGuid, String name, String description, ActionStateOptions state, String start){
    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = env + "/api/status/notify/";
    } else {
      System.out.println(" ");
      System.out.println("==========Warning===========");
      System.out.println(
          "ENVIRONMENT VARIABLE 'PREINGEST_WEBAPI' is not found. Please add ENVIRONMENT VARIABLE with protocol+servername(or server ip) without forward slash.");
      System.out.println("Failed result will not be register in the local database.");
      System.out.println("============================");
      System.out.println(" ");
      return;
    }

    try {
      BodyNotifyEventMessage body = new BodyNotifyEventMessage();
      body.message = description;
      body.name = name;
      body.state = state.name();
      body.sessionId = folderGuid;
      body.hasSummary = false;
      ZonedDateTime end = ZonedDateTime.now(ZoneId.of("UTC"));         
      String doneDateTimeNow = end.format(_formatter);

      if(state == ActionStateOptions.Completed){
        body.hasSummary = true;
        body.processed = 1;
        body.accepted = 1;
        body.rejected = 0;
        body.start = start+"+00:00";
        body.end = doneDateTimeNow+"+00:00";
      }
      if(state == ActionStateOptions.Failed){
        body.hasSummary = true;
        body.processed = 1;
        body.accepted = 0;
        body.rejected = 1;
        body.start = start+"+00:00";
        body.end = doneDateTimeNow+"+00:00";
      }

      //2021-01-13 16:48:30 +0000
      //2021-01-11T16:58:36.4297321+00:00    
      //2021-01-13T17:42:19.741+0000
      //2021-01-13T17:52:09.6147437+0000
      ZonedDateTime currentNow = ZonedDateTime.now(ZoneId.of("UTC"));  
      String dateTimeNow = currentNow.format(_formatter);
      body.eventDateTime = dateTimeNow+"+00:00";

      RestTemplate restTemplate = new RestTemplate();
      BodyNotifyEventMessage result = restTemplate.postForObject(url, body, BodyNotifyEventMessage.class);
      if (result != null) {
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();
        String json = ow.writeValueAsString(result);
        System.out.println(json);
      }
    } catch (Exception e) {
      e.printStackTrace();
    }
  }

  protected void registerStartStatus(String actionGuid) {

    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = env + "/api/status/start/" + actionGuid;
    } else {
      System.out.println(" ");
      System.out.println("==========Warning===========");
      System.out.println(
          "ENVIRONMENT VARIABLE 'PREINGEST_WEBAPI' is not found. Please add ENVIRONMENT VARIABLE with protocol+servername(or server ip) without forward slash.");
      System.out.println("Start result will not be register in the local database.");
      System.out.println("============================");
      System.out.println(" ");
    }

    try {
      RestTemplate restTemplate = new RestTemplate();
      NewResultItem result = restTemplate.postForObject(url, null, NewResultItem.class);

      if (result != null) {
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();
        String json = ow.writeValueAsString(result);
        System.out.println(json);
      }
    } catch (Exception e) {
      e.printStackTrace();
    }
  }

  protected void registerCompletedStatus(String actionGuid) {

    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = env + "/api/status/completed/" + actionGuid;
    } else {
      System.out.println(" ");
      System.out.println("==========Warning===========");
      System.out.println(
          "ENVIRONMENT VARIABLE 'PREINGEST_WEBAPI' is not found. Please add ENVIRONMENT VARIABLE with protocol+servername(or server ip) without forward slash.");
      System.out.println("Completed result will not be register in the local database.");
      System.out.println("============================");
      System.out.println(" ");
    }

    try {
      RestTemplate restTemplate = new RestTemplate();
      NewResultItem result = restTemplate.postForObject(url, null, NewResultItem.class);

      if (result != null) {
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();
        String json = ow.writeValueAsString(result);
        System.out.println(json);
      }
    } catch (Exception e) {
      e.printStackTrace();
    }
  }

  protected void registerFailedStatus(String actionGuid, BodyMessage message) {

    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = env + "/api/status/failed/" + actionGuid;
    } else {
      System.out.println(" ");
      System.out.println("==========Warning===========");
      System.out.println(
          "ENVIRONMENT VARIABLE 'PREINGEST_WEBAPI' is not found. Please add ENVIRONMENT VARIABLE with protocol+servername(or server ip) without forward slash.");
      System.out.println("Failed result will not be register in the local database.");
      System.out.println("============================");
      System.out.println(" ");
      return;
    }

    try {
      RestTemplate restTemplate = new RestTemplate();
      RootMessage result = restTemplate.postForObject(url, message, RootMessage.class);
      if (result != null) {
        ObjectWriter ow = new ObjectMapper().writer().withDefaultPrettyPrinter();
        String json = ow.writeValueAsString(result);
        System.out.println(json);
      }
    } catch (Exception e) {
      e.printStackTrace();
    }
  }

  private UpdateResult executeLogic(String guid, String handlerName, String[] commandArgs) {
    
    UpdateResult result = UpdateResult.Error;

    try {
      ProcessBuilder builder = new ProcessBuilder(commandArgs);
      Process process = builder.start();

      Thread ioThread = new Thread() {
        @Override
        public void run() {
          try {
              ZonedDateTime end = ZonedDateTime.now(ZoneId.of("UTC"));         
              String startDateTimeNow = end.format(_formatter);

              final BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
              String line = null;
              while ((line = reader.readLine()) != null) {
                System.out.println(line);
              
              notifyClient(guid, handlerName, line, ActionStateOptions.Executing, startDateTimeNow);
            }
            reader.close();
          } catch (final Exception e) {
            e.printStackTrace();
          } finally {}

        }
      };

      ioThread.start();

      try {
        process.waitFor();
        result = UpdateResult.Success;
      } catch (InterruptedException ie) {
        ie.printStackTrace();
        result = UpdateResult.Failed;
      }

    } catch (Exception e) {
      e.printStackTrace();
      result = UpdateResult.Failed;
    } finally { }

    return result;
  }

  public boolean copyProfileTemplate(String guid) {
    boolean result = false;
    try {
      Path source = Paths.get(String.format("%1$2stemplate-linux.droid", new Object[] { this.DROID_LINUX_FOLDER }),
          new String[0]);
      Path target = Paths.get(
          // String.format("%1$2s%2$2s/%2$2s.droid", new Object[] {
          // this.ARCHIVEDATA_LINUX_FOLDER, guid }),
          String.format("%1$2s%2$2s/%3$2s.droid",
              new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, guid, "DroidValidationHandler" }),
          new String[0]);

      Files.copy(source, target, new CopyOption[] { StandardCopyOption.REPLACE_EXISTING });
      result = true;
    }

    catch (IOException ioe) {
      System.out.println("Java profile file copy (linux): IOException occured.");
      ioe.printStackTrace();
      result = false;
    }

    return result;
  }

  public NewActionResult execute() throws Exception {
    System.out.println("Java CommandHandler (execute): Not implemented.");
    throw new Exception("Function is not implemented.");
  }
}
