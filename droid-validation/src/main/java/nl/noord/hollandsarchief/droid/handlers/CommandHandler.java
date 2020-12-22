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

import nl.noord.hollandsarchief.droid.entities.NewActionResult;

import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URL;

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
    runSeperateThread("", commandArgs);
  }

  protected void runSeperateThread(String guid, String[] commandArgs) throws IOException {

    System.out.println(" ");
    System.out.println("==========Arguments Passed From Command line===========");
    for (String args : commandArgs)
      System.out.println(args);
    System.out.println("============================");
    System.out.println(" ");

    if (guid == null || guid.length() == 0) {
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
      String jsonData = "{ \"name\" : \"Droid\", \"description\" : \"Test\", \"result\" : \"Test\" }";
      String requestResult = registerNewAction(guid, jsonData);

      if (requestResult != null) {
        if (requestResult.length() > 0) {
          NewActionResult actionResult = new ObjectMapper().readValue(requestResult, NewActionResult.class);

          final Thread mainThread = new Thread() {
            @Override
            public void run() {
              boolean isError = false;
              try {
                // send start signal
                if (actionResult != null) {
                  String actionGuid = actionResult.getProcessId();
                  registerStartStatus(actionGuid);
                }
                executeLogic(commandArgs);
              } catch (final Exception e) {
                // send error signal
                // return of course
                e.printStackTrace();
                if (actionResult != null) {
                  String actionGuid = actionResult.getProcessId();
                  registerFailedStatus(actionGuid, e.getMessage());
                }
                isError = true;
              } finally {
                // send completed signal
                if (actionResult != null && !isError) {
                  String actionGuid = actionResult.getProcessId();
                  registerCompletedStatus(actionGuid);
                }
              }
            }
          };
          mainThread.start();
        }
      }
    }
  }

  private String registerNewAction(String folderGuid, String jsonData) {
    String dev = "localhost:55004";
    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = "http://" + env + "/api/status/new/" + folderGuid;
    } else {
      url = "https://" + dev + "/api/status/new/" + folderGuid;
    }

    String result = postPreingestStatus(url, jsonData);

    return result;
  }

  private void registerStartStatus(String actionGuid) {
    String dev = "localhost:55004";
    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = "http://" + env + "/api/status/start/" + actionGuid;
    } else {
      url = "https://" + dev + "/api/status/start/" + actionGuid;
    }
    postPreingestStatus(url, "");
  }

  private void registerCompletedStatus(String actionGuid) {
    String dev = "localhost:55004";
    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = "http://" + env + "/api/status/completed/" + actionGuid;
    } else {
      url = "https://" + dev + "/api/status/completed/" + actionGuid;
    }
    postPreingestStatus(url, "");
  }

  private void registerFailedStatus(String actionGuid, String message) {
    String dev = "localhost:55004";
    String env = System.getenv("PREINGEST_WEBAPI");
    String url = "";
    if (env != null && env.length() > 0) {
      url = "http://" + env + "/api/status/failed/" + actionGuid;
    } else {
      url = "https://" + dev + "/api/status/failed/" + actionGuid;
    }
    postPreingestStatus(url, message);
  }

  private String postPreingestStatus(String preingestUrl, String input) {
    String result = "";

    try {
      URL url = new URL(preingestUrl);
      HttpURLConnection conn = (HttpURLConnection) url.openConnection();
      conn.setDoOutput(true);
      conn.setRequestMethod("POST");
      conn.setRequestProperty("Content-Type", "application/json");

      OutputStream os = conn.getOutputStream();
      os.write(input.getBytes());
      os.flush();

      if (conn.getResponseCode() != HttpURLConnection.HTTP_CREATED) {
        throw new RuntimeException("Failed : HTTP error code : " + conn.getResponseCode());
      }

      BufferedReader br = new BufferedReader(new InputStreamReader((conn.getInputStream())));

      String output;
      System.out.println("Output from Server .... \n");
      while ((output = br.readLine()) != null) {
        System.out.println(output);
      }

      conn.disconnect();

    } catch (MalformedURLException e) {
      e.printStackTrace();
    } catch (IOException e) {
      e.printStackTrace();
    }

    return result;
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
          String.format("%1$2s%2$2s/%2$2s.droid", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, guid }), new String[0]);

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

  public void execute() {
    System.out.println("Java CommandHandler (execute): Not implemented.");
  }
}
