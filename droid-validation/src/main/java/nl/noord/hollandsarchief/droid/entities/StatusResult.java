package nl.noord.hollandsarchief.droid.entities;

public class StatusResult {
  private final String _message;
  private final boolean _result;
  
  public StatusResult(String message, boolean result) {
    this._message = message;
    this._result = result;
  }
  
  public String getMessage() {
    return this._message;
  }
  
  public boolean getResult() {
    return this._result;
  }
}
