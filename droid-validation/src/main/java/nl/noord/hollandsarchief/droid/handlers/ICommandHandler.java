package nl.noord.hollandsarchief.droid.handlers;

public interface ICommandHandler {
  String currentApplicationLocation();
  
  boolean existsArchiveFolder();
  
  boolean existsDroidFolder();
  
  void execute();
}
