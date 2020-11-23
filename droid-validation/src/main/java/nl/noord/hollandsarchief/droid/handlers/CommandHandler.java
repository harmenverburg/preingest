package nl.noord.hollandsarchief.droid.handlers;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.file.CopyOption;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;

public abstract class CommandHandler implements ICommandHandler
{
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

  
  protected void runSeperateThread(String[] commandArgs, boolean wait) throws IOException {
    System.out.println(" ");
    System.out.println("==========Arguments Passed From Command line===========");   
    for(String args : commandArgs)
      System.out.println(args);    
    System.out.println("============================");
    System.out.println(" ");
    ProcessBuilder builder = new ProcessBuilder(commandArgs);
   
    final Process process = builder.start();    
    final Thread ioThread = new Thread() {
        @Override
        public void run() {
            try {
                final BufferedReader reader = new BufferedReader(
                        new InputStreamReader(process.getInputStream()));
                String line = null;
                while ((line = reader.readLine()) != null) {
                    System.out.println(line);
                }
                reader.close();
            } catch (final Exception e) {
                e.printStackTrace();
            }
        }
    };
    ioThread.start();

    try{
      if(wait)
      process.waitFor();
    }
    catch(InterruptedException ie){
      ie.printStackTrace();
    }
  }
  
  protected boolean copyProfileTemplate(String guid) {
    boolean result = false;
    try {
      Path source = Paths.get(String.format("%1$2stemplate-linux.droid", new Object[] { this.DROID_LINUX_FOLDER }), new String[0]);
      Path target = Paths.get(String.format("%1$2s%2$2s/%2$2s.droid", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, guid }), new String[0]);
      
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
