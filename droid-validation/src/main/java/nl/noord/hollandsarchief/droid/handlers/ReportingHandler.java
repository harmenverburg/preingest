package nl.noord.hollandsarchief.droid.handlers;

import java.io.IOException;

public class ReportingHandler extends CommandHandler
{
  private String _guid = null;
  
  public ReportingHandler(String guid) {
    this._guid = guid;
  }
  public void execute() {
    doPdf();
    doDroid();
    doPlanets();
  }

  public void doPdf() {
    String[] command = {
        
        "java", 
        "-jar", 
        String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER
          }), "-r", 
        String.format("%1$2s%2$2s/%2$2s.pdf", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid
          }), "-n", 
        "\"Comprehensive breakdown\"", 
        "-p", 
        String.format("%1$2s%2$2s/%2$2s.droid", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
      };
    
    try{
      if (command.length > 0)
      runSeperateThread(command, false); 
    }
    catch(IOException ioe){
      ioe.printStackTrace();
    }
  }

  public void doDroid() {
    String[] command = {
        
        "java", 
        "-jar", 
        String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER
          }), "-r", 
        String.format("%1$2s%2$2s/%2$2s.droid.xml", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid
          }), "-t", 
        "\"DROID Report XML\"", 
        "-n", 
        "\"Comprehensive breakdown\"", 
        "-p", 
        String.format("%1$2s%2$2s/%2$2s.droid", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
      };
    
      try{
        if (command.length > 0)
        runSeperateThread(command, false); 
      }
      catch(IOException ioe){
        ioe.printStackTrace();
      }
  }

  public void doPlanets() {
    String[] command = {
        
        "java", 
        "-jar", 
        String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER
          }), "-r", 
        String.format("%1$2s%2$2s/%2$2s.planets.xml", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid
          }), "-t", 
        "\"Planets XML\"", 
        "-n", 
        "\"Comprehensive breakdown\"", 
        "-p", 
        String.format("%1$2s%2$2s/%2$2s.droid", new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
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

