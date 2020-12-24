package nl.noord.hollandsarchief.droid.handlers;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.file.CopyOption;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.ObjectWriter;

import org.springframework.web.client.RestTemplate;
import nl.noord.hollandsarchief.droid.entities.NewActionResult;
import nl.noord.hollandsarchief.droid.entities.NewResultItem;
import nl.noord.hollandsarchief.droid.entities.RootMessage;
import nl.noord.hollandsarchief.droid.entities.BodyMessage;
import nl.noord.hollandsarchief.droid.entities.BodyAction;

public abstract class CommandHandler implements ICommandHandler {
  protected String ARCHIVEDATA_LINUX_FOLDER = "/data/";
  protected String DROID_LINUX_FOLDER = "/droid/";

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

  protected void runSeperateThread(String[] commandArgs) throws IOException {
    runSeperateThread(null, null, commandArgs);
  }

  protected void runSeperateThread(String processGuid, String guid, String[] commandArgs) throws IOException {

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
          try {
            executeLogic(commandArgs);
          } catch (final Exception e) {
            e.printStackTrace();
          } finally {
          }
        }
      };
      mainThread.start();
    } else {

      final Thread mainThread = new Thread() {
        @Override
        public void run() {
          boolean isError = false;

          try {
            // send start signal
            if (processGuid != null) {
              String actionGuid = processGuid;
              registerStartStatus(actionGuid);
            }
            executeLogic(commandArgs);            
          } catch (final Exception e) {
            // send error signal
            // return of course
            e.printStackTrace();
            if (processGuid != null) {
              BodyMessage message = new BodyMessage();
              message.message = e.getMessage();
              String actionGuid = processGuid;
              registerFailedStatus(actionGuid, message);
            }
            isError = true;
          } finally {
            // send completed signal
            if (processGuid != null && !isError) {
              String actionGuid = processGuid;
              registerCompletedStatus(actionGuid);
            }
          }
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

  private void executeLogic(String[] commandArgs) {
    try {
      ProcessBuilder builder = new ProcessBuilder(commandArgs);
      Process process = builder.start();

      Thread ioThread = new Thread() {
        @Override
        public void run() {
          try {

            final BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
            String line = null;
            while ((line = reader.readLine()) != null) {
              System.out.println(line);
            }
            reader.close();
          } catch (final Exception e) {
            e.printStackTrace();
          } finally {
          }
        }
      };

      ioThread.start();

      try {
        process.waitFor();
      } catch (InterruptedException ie) {
        ie.printStackTrace();
      }
    } catch (Exception e) {
      e.printStackTrace();
    } finally {
    }

  }

  protected boolean copyProfileTemplate(String guid) {
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
