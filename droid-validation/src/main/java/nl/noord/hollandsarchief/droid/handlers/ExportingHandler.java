package nl.noord.hollandsarchief.droid.handlers;

import java.io.IOException;

import nl.noord.hollandsarchief.droid.entities.BodyAction;
import nl.noord.hollandsarchief.droid.entities.NewActionResult;

public class ExportingHandler extends CommandHandler {
  private String _guid = null;

  public ExportingHandler(String guid) {
    this._guid = guid;
  }

  public NewActionResult execute() {
    NewActionResult result = null;

    String[] command = { "java", "-jar",
        String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), "-R", "-p",
        // String.format("%1$2s%2$2s/%2$2s.droid", new Object[] {
        // this.ARCHIVEDATA_LINUX_FOLDER, this._guid }),
        String.format("%1$2s%2$2s/%3$2s.droid",
            new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid, "DroidValidationHandler" }),
        "-e",
        // String.format("%1$2s%2$2s/%2$2s.droid.csv", new Object[] {
        // this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
        String.format("%1$2s%2$2s/%3$2s.csv",
            new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid, "DroidValidationHandler" }) };

    try {
      if (command.length > 0) {
        BodyAction jsonData = new BodyAction();
        jsonData.name =  ExportingHandler.class.getName() + " - Droid CSV report";
        jsonData.description = String.join(" ", command);
        jsonData.result = "DroidValidationHandler.csv";

        result = this.registerNewAction(this._guid, jsonData);
        String processId = result != null ? result.processId : null;
        runSeperateThread(ExportingHandler.class.getName(), processId, this._guid, command);
      }
    } catch (IOException ioe) {
      ioe.printStackTrace();
    }

    return result;
  }
}
