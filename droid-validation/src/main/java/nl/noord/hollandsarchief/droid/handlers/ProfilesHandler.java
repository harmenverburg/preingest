package nl.noord.hollandsarchief.droid.handlers;

import java.io.File;
import java.io.IOException;

public class ProfilesHandler extends CommandHandler {
  private String _guid = null;
  
  public ProfilesHandler(String guid) {
    this._guid = guid;
  }
  
  public void execute() {
    boolean copyResult = copyProfileTemplate(this._guid);
    
    if (copyResult) {      
      String sessiondId = String.format("%1$2s%2$2s", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid });
      String collectionName = "";
      
      File collection = new File(sessiondId);
      String[] contents = collection.list();
      for (int i = 0; i < contents.length; i++) {
        boolean isDirectory = (new File(String.valueOf(sessiondId) + "/" + contents[i])).isDirectory();
        if (isDirectory) {
          collectionName = contents[i];          
          break;
        } 
      } 
      String[] command = {          
          "java", 
          "-jar", 
          String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), 
            "-R", 
          "-a", 
          String.format("\"%1$2s%2$2s/%3$2s\"", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid, collectionName }), 
            "-p", 
          String.format("\"%1$2s%2$2s/%2$2s.droid\"", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
        };
      
        try{
          if (command.length > 0)
          runSeperateThread(this._guid, command); 
        }
        catch(IOException ioe){
          ioe.printStackTrace();
        }
    } 
  }
}

