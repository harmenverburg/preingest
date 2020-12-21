package nl.noord.hollandsarchief.droid.entities;

import java.time.LocalDateTime;

public class NewActionResult {
  private String _processId;
  private String _folderSessionId;
  private String _name;
  private String _description;
  private LocalDateTime _creation;
  
  public NewActionResult() {}

  public String getName() {
    return _name;
  }

  public void setName(String value) {
    this._name = value;
  }

  public String getProcessId() {
    return _processId;
  }

  public void setProcessId(String value) {
    this._processId = value;
  }

  public String getFolderSessionId() {
    return _folderSessionId;
  }

  public void setFolderSessionId(String value) {
    this._folderSessionId = value;
  }

  public String getDescription() {
    return _description;
  }

  public void setDescription(String value) {
    this._description = value;
  }

  public LocalDateTime getCreation() {
    return _creation;
  }

  public void setCreation(LocalDateTime value) {
    this._creation = value;
  }

}