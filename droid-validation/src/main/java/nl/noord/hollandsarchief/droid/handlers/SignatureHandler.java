package nl.noord.hollandsarchief.droid.handlers;

import java.io.IOException;

public class SignatureHandler extends CommandHandler {
  
  public void execute() {
    doUpdateCheck();
    doDownloadUpdate();
  }
  
  private void doUpdateCheck() {
    String[] command = null;
    command = new String[] { "java", "-jar", String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), "-c" };
    
    try{
      if (command.length > 0)
      runSeperateThread(command, true); 
    }
    catch(IOException ioe){
      ioe.printStackTrace();
    }
  }
  
  private void doDownloadUpdate() {
    String[] command = null;
    command = new String[] { "java", "-jar", String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), "-d" };
    
    try{
      if (command.length > 0)
      runSeperateThread(command, false); 
    }
    catch(IOException ioe){
      ioe.printStackTrace();
    }
  }
}

