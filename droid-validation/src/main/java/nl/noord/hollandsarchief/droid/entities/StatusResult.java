package nl.noord.hollandsarchief.droid.entities;

public class StatusResult {
  public String message;
  public boolean result;
  public String actionId;
  
  public StatusResult(String message, boolean result, String actionId) {
    this.message = message;
    this.result = result;
    this.actionId = actionId;
  }
}
