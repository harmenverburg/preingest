package nl.noord.hollandsarchief.droid.handlers;

import java.io.IOException;

import nl.noord.hollandsarchief.droid.entities.BodyAction;
import nl.noord.hollandsarchief.droid.entities.NewActionResult;

public class ReportingPlanetsXmlHandler extends CommandHandler {
    private String _guid = null;

    public ReportingPlanetsXmlHandler(String guid) {
        this._guid = guid;
    }

    public NewActionResult execute() {
        NewActionResult result = null;
        String[] command = { "java", "-jar",
                String.format("%1$2sdroid-command-line-6.5.jar", new Object[] { this.DROID_LINUX_FOLDER }), "-r",
                // String.format("%1$2s%2$2s/%2$2s.planets.xml", new Object[] {
                // this.ARCHIVEDATA_LINUX_FOLDER, this._guid }),
                String.format("%1$2s%2$2s/%3$2s.planets.xml",
                        new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid, "DroidValidationHandler" }),
                "-t", "\"Planets XML\"", "-n", "\"Comprehensive breakdown\"", "-p",
                // String.format("%1$2s%2$2s/%2$2s.droid", new Object[] {
                // this.ARCHIVEDATA_LINUX_FOLDER, this._guid })
                String.format("%1$2s%2$2s/%3$2s.droid",
                        new Object[] { this.ARCHIVEDATA_LINUX_FOLDER, this._guid, "DroidValidationHandler" }) };
        try {
            if (command.length > 0) {
                BodyAction jsonData = new BodyAction();
                jsonData.name = ReportingPlanetsXmlHandler.class.getSimpleName();
                jsonData.description = String.join(" ", command);
                jsonData.result = "DroidValidationHandler.planets.xml";

                result = this.registerNewAction(this._guid, jsonData);
                String processId = result != null ? result.processId : null;
                runSeperateThread(ReportingPlanetsXmlHandler.class.getSimpleName(), processId, this._guid, command);
            }
        } catch (IOException ioe) {
            ioe.printStackTrace();
        }

        return result;
    }

}
