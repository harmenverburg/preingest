using Microsoft.Extensions.Logging;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 2.2
    public class NamingValidationHandler : AbstractPreingestHandler
    {

        public NamingValidationHandler(AppSettings settings) : base(settings)
        {

        }

        public override void Execute()
        {
            string targetFolder = Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString());

            var collection = new DirectoryInfo(targetFolder).GetDirectories().First();
            if (collection == null)
                return;

            var result = new List<ProcessResult>();

            DirectoryRecursion(collection, result);

            SaveJson(new DirectoryInfo(targetFolder), this, result.ToArray());
        }

        private void DirectoryRecursion(DirectoryInfo currentFolder, List<ProcessResult> procesResult)
        {
            this.Logger.LogDebug("Checking folder '{0}'", currentFolder.FullName);

            bool checkResult = ContainsInvalidCharacters(currentFolder.Name);
            if (checkResult)
            {
                //DNA_CSM_023	Een of meerdere niet-toegestane bijzondere tekens komen voor in de map- of bestandsnaam <padnaam map of bestand>. 
                ProcessResult item = new ProcessResult (SessionGuid)
                {
                    CollectionItem = currentFolder.Name,
                    Code = "Not;OK;InvalidCharacter",
                    CreationTimestamp = DateTime.Now,
                    ActionName = this.GetType().Name,
                    Message = String.Format("Een of meerdere niet-toegestane bijzondere tekens komen voor in de map - of bestandsnaam '{0} ({1})'.", currentFolder.Name, currentFolder.FullName),
                };
                procesResult.Add(item);
            }

            bool checkResultNames = ContainsAnyDOSNames(currentFolder.Name);
            if (checkResultNames)
            {
                //DNA_CSM_024 De map of het bestand<padnaam map of bestand> heeft een niet-toegestane naam.
                ProcessResult item = new ProcessResult(SessionGuid)
                {
                    CollectionItem = currentFolder.Name,
                    Code = "Not;OK;DOSCommand",
                    CreationTimestamp = DateTime.Now,
                    ActionName = this.GetType().Name,
                    Message = String.Format("De map of het bestand '{0} ({1})' heeft een niet-toegestane naam.", currentFolder.Name, currentFolder.FullName),
                };
                procesResult.Add(item);
            }

            currentFolder.GetFiles().ToList().ForEach(item =>
            {
                this.Logger.LogDebug("Checking file '{0}'", currentFolder.FullName);

                bool checkResult = ContainsInvalidCharacters(item.Name);
                if (checkResult)
                {
                    //DNA_CSM_023	Een of meerdere niet-toegestane bijzondere tekens komen voor in de map- of bestandsnaam <padnaam map of bestand>.
                    ProcessResult result = new ProcessResult(SessionGuid)
                    {
                        CollectionItem = currentFolder.Name,
                        Code = "Not;OK;InvalidCharacter",
                        CreationTimestamp = DateTime.Now,
                        ActionName = this.GetType().Name,
                        Message = String.Format("Een of meerdere niet-toegestane bijzondere tekens komen voor in de map - of bestandsnaam '{0} ({1})'", item.Name, item.FullName),
                    };
                    procesResult.Add(result);
                }

                bool checkResultNames = ContainsAnyDOSNames(item.Name);
                if (checkResultNames)
                {
                    // DNA_CSM_024 De map of het bestand<padnaam map of bestand> heeft een niet-toegestane naam.
                    ProcessResult result = new ProcessResult(SessionGuid)
                    {
                        CollectionItem = currentFolder.Name,
                        Code = "Not;OK;DOSCommand",
                        CreationTimestamp = DateTime.Now,
                        ActionName = this.GetType().Name,
                        Message = String.Format("De map of het bestand '{0} ({1})' heeft een niet-toegestane naam.", item.Name, item.FullName),
                    };
                    procesResult.Add(result);
                }
            });

            foreach (var directory in currentFolder.GetDirectories())
                DirectoryRecursion(directory, procesResult);
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
