package nl.noord.hollandsarchief.droid.handlers;

import java.io.IOException;

public class ExportingHandler extends CommandHandler {
  private String _guid = null;
  
  public ExportingHandler(String guid) {
    this._guid = guid;
  }
  
  public void execute() {
    String[] command = {        
        "java", 
        "-jar", 
        String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER
          }), "-R", 
        "-p", 
        String.format("%1$2s%2$2s/%2$2s.droid", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid
          }), "-e", 
        String.format("%1$2s%2$2s/%2$2s.droid.csv", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
      };    
    
    try{
      if (command.length > 0)
      runSeperateThread(command, false); 
    }
    catch(IOException ioe){
      ioe.printStackTrace();
    }
  }
}

