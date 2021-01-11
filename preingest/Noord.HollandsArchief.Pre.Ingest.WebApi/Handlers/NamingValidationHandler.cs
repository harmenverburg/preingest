using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 2.2
    public class NamingValidationHandler : AbstractPreingestHandler
    {
        public NamingValidationHandler(AppSettings settings) : base(settings) { }
        public override void Execute()
        {
            bool isSucces = false;
            var anyMessages = new List<String>();
            var eventModel = CurrentActionProperties(TargetCollection, this.GetType().Name);
            try
            {  
                OnTrigger(new PreingestEventArgs { Description=String.Format("Start name check on folders, sub-folders and files in '{0}'", TargetFolder),  Initiate = DateTime.Now, ActionType = PreingestActionStates.Started, PreingestAction = eventModel });

                var collection = new DirectoryInfo(TargetFolder).GetDirectories().First();
                if (collection == null)
                    throw new DirectoryNotFoundException(String.Format("Directory '{0}' not found!", TargetFolder));

                var result = new List<NamingItem>();

                DirectoryRecursion(collection, result, new PreingestEventArgs { Description = "Walk through the folder structure.", Initiate = DateTime.Now, ActionType = PreingestActionStates.Executing, PreingestAction = eventModel });

                eventModel.Summary.Processed = result.Count();
                eventModel.Summary.Accepted = result.Where(item => !item.ContainsDosNames && !item.ContainsInvalidCharacters).Count();
                eventModel.Summary.Rejected = result.Where(item => item.ContainsDosNames || item.ContainsInvalidCharacters).Count();

                eventModel.ActionData = result.ToArray();

                if (eventModel.Summary.Rejected > 0)
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Error;
                else
                    eventModel.ActionResult.ResultValue = PreingestActionResults.Success;

                isSucces = true;
            }
            catch(Exception e)
            {
                isSucces = false;
                Logger.LogError(e, "An exception occured in file and folder name check!");
                anyMessages.Clear();
                anyMessages.Add("An exception occured in file and folder name check!");
                anyMessages.Add(e.Message);
                anyMessages.Add(e.StackTrace);

                //eventModel.Summary.Processed = -1;
                eventModel.Summary.Accepted = 0;
                eventModel.Summary.Rejected = eventModel.Summary.Processed;

                eventModel.ActionResult.ResultValue = PreingestActionResults.Failed;
                eventModel.Properties.Messages = anyMessages.ToArray();

                OnTrigger(new PreingestEventArgs { Description = "An exception occured in name check!", Initiate = DateTime.Now, ActionType = PreingestActionStates.Failed, PreingestAction = eventModel });
            }
            finally
            {
                if (isSucces)
                    OnTrigger(new PreingestEventArgs { Description = "Checking names in files and folders is done.", Initiate = DateTime.Now, ActionType = PreingestActionStates.Completed, PreingestAction = eventModel });
            }
        }

        private void DirectoryRecursion(DirectoryInfo currentFolder, List<NamingItem> procesResult, PreingestEventArgs model)
        {
            model.Description = String.Format ("Checking folder '{0}'.", currentFolder.FullName);            
            OnTrigger(model);

            this.Logger.LogDebug("Checking folder '{0}'.", currentFolder.FullName);

            bool checkResult = ContainsInvalidCharacters(currentFolder.Name);
            bool checkResultNames = ContainsAnyDOSNames(currentFolder.Name);
            var errorMessages = new List<String>();
            if (checkResult)            
                errorMessages.Add(String.Format("Een of meerdere niet-toegestane bijzondere tekens komen voor in de map - of bestandsnaam '{0} ({1})'.", currentFolder.Name, currentFolder.FullName));  
            if (checkResultNames)           
                errorMessages.Add(String.Format("De map of het bestand '{0} ({1})' heeft een niet-toegestane naam.", currentFolder.Name, currentFolder.FullName));

            procesResult.Add(new NamingItem { ContainsInvalidCharacters = checkResult, ContainsDosNames = checkResultNames, Name = currentFolder.FullName, ErrorMessages = errorMessages.ToArray() });

            currentFolder.GetFiles().ToList().ForEach(item =>
            {
                model.Description = String.Format("Checking file '{0}'.", item.FullName);                
                OnTrigger(model);

                this.Logger.LogDebug("Checking file '{0}'", currentFolder.FullName);
                var errorMessages = new List<String>();
                bool checkResult = ContainsInvalidCharacters(item.Name);
                if (checkResult)                
                    errorMessages.Add(String.Format("Een of meerdere niet-toegestane bijzondere tekens komen voor in de map - of bestandsnaam '{0} ({1})'", item.Name, item.FullName));
                bool checkResultNames = ContainsAnyDOSNames(item.Name);
                if (checkResultNames)                
                    errorMessages.Add(String.Format("De map of het bestand '{0} ({1})' heeft een niet-toegestane naam.", item.Name, item.FullName));

                procesResult.Add(new NamingItem { ContainsInvalidCharacters = checkResult, ContainsDosNames = checkResultNames, Name = item.FullName, ErrorMessages = errorMessages.ToArray() });
            });

            foreach (var directory in currentFolder.GetDirectories())
                DirectoryRecursion(directory, procesResult, model);
        }

        private bool ContainsInvalidCharacters(string testName)
        {
            Regex containsABadCharacter = new Regex("[\\?*:\"​|/<>#&‌​]");
            return (containsABadCharacter.IsMatch(testName));
        }

        private bool ContainsAnyDOSNames(string testName)
        {
            Regex containsAnyDOSNames = new Regex("^(PRN|AUX|NUL|CON|COM[0-9]|LPT[0-9]|(\\.+)$)", RegexOptions.IgnoreCase);
            return (containsAnyDOSNames.IsMatch(testName));
        }
    }
}
