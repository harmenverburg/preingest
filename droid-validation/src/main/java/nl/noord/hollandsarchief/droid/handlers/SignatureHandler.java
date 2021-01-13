package nl.noord.hollandsarchief.droid.handlers;

import java.io.IOException;
import nl.noord.hollandsarchief.droid.entities.NewActionResult;

public class SignatureHandler extends CommandHandler {
  
  public NewActionResult execute() throws Exception {
    throw new Exception ("Method is not implemented. Use instead doUpdateCheck, doDownloadUpdate.");
  }
  
  public void doUpdateCheck() {
    String[] command = null;
    command = new String[] { "java", "-jar", String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), "-c" };
    
    try{
      if (command.length > 0){
        runSeperateThread(SignatureHandler.class.getName(), command);
      }
    }
    catch(IOException ioe){
      ioe.printStackTrace();
    }

  }
  
  public void doDownloadUpdate() {
    String[] command = null;
    command = new String[] { "java", "-jar", String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), "-d" };
    
    try{
      if (command.length > 0){
        runSeperateThread(SignatureHandler.class.getName(), command);
      }
    }
    catch(IOException ioe){
      ioe.printStackTrace();
    }   
  }
}

