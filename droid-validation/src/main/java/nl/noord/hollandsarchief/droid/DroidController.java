package nl.noord.hollandsarchief.droid;

import nl.noord.hollandsarchief.droid.entities.StatusResult;
import nl.noord.hollandsarchief.droid.handlers.ExportingHandler;
import nl.noord.hollandsarchief.droid.handlers.ProfilesHandler;
import nl.noord.hollandsarchief.droid.handlers.ReportingHandler;
import nl.noord.hollandsarchief.droid.handlers.SignatureHandler;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;


@RestController
@CrossOrigin
@RequestMapping({"api/droid/v6.5"})
public class DroidController
{
  @GetMapping({"/"})
  public StatusResult home() {

    String env = System.getenv("PREINGEST_WEBAPI");

    if(env == null)
      env = "value is empty! Please start process with environment variable 'PREINGEST_WEBAPI'";
    
    return new StatusResult("Service is available, Status : Running, Environment to preingest webapi : "+env, true);
  }

  
  @GetMapping({"/profiles/{guid}"})
  public StatusResult profiled(@PathVariable String guid) {
    ProfilesHandler handler = new ProfilesHandler(guid);
    if (!handler.existsArchiveFolder()) {
      return new StatusResult("Archive folder not found!", false);
    }
    if (!handler.existsDroidFolder()) {
      return new StatusResult("Droid folder not found!", false);
    }
    handler.execute();
    
    return new StatusResult("Adding and preparing resource.", true);
  }
  
  @GetMapping({"/reporting/pdf/{guid}"})
  public StatusResult reportingPdf(@PathVariable String guid) {
    ReportingHandler handler = new ReportingHandler(guid);
    
    if (!handler.existsArchiveFolder()) {
      return new StatusResult("Archive folder not found!", false);
    }
    if (!handler.existsDroidFolder()) {
      return new StatusResult("Droid folder not found!", false);
    }
    
    handler.doPdf();
    
    return new StatusResult("Generating results.", true);
  }
  
  @GetMapping({"/reporting/droid/{guid}"})
  public StatusResult reportingDroid(@PathVariable String guid) {
    ReportingHandler handler = new ReportingHandler(guid);
    
    if (!handler.existsArchiveFolder()) {
      return new StatusResult("Archive folder not found!", false);
    }
    if (!handler.existsDroidFolder()) {
      return new StatusResult("Droid folder not found!", false);
    }
    
    handler.doDroid();
    
    return new StatusResult("Generating results.", true);
  }
  
  @GetMapping({"/reporting/planets/{guid}"})
  public StatusResult reportingPlanets(@PathVariable String guid) {
    ReportingHandler handler = new ReportingHandler(guid);
    
    if (!handler.existsArchiveFolder()) {
      return new StatusResult("Archive folder not found!", false);
    }
    if (!handler.existsDroidFolder()) {
      return new StatusResult("Droid folder not found!", false);
    }
    
    handler.doPlanets();
    
    return new StatusResult("Generating results.", true);
  }
  
  @GetMapping({"/exporting/{guid}"})
  public StatusResult exporting(@PathVariable String guid) {
    ExportingHandler handler = new ExportingHandler(guid);
    
    if (!handler.existsArchiveFolder()) {
      return new StatusResult("Archive folder not found!", false);
    }
    if (!handler.existsDroidFolder()) {
      return new StatusResult("Droid folder not found!", false);
    }
    
    handler.execute();
    
    return new StatusResult("Exporting results.", true);
  }
  
  @GetMapping({"/signature/update"})
  public StatusResult signature() {
    SignatureHandler handler = new SignatureHandler();
    
    if (!handler.existsArchiveFolder()) {
      return new StatusResult("Archive folder not found!", false);
    }
    if (!handler.existsDroidFolder()) {
      return new StatusResult("Droid folder not found!", false);
    }
    
    handler.execute();
    
    return new StatusResult("Signature results.", true);
  }
}
