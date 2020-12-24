package nl.noord.hollandsarchief.droid.handlers;

import nl.noord.hollandsarchief.droid.entities.NewActionResult;

public interface ICommandHandler {
  String currentApplicationLocation();
  
  boolean existsArchiveFolder();
  
  boolean existsDroidFolder();
  
  NewActionResult execute() throws Exception;
}
