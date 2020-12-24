using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    public class GreenListHandler : AbstractPreingestHandler
    {
        public GreenListHandler(AppSettings settings) : base(settings)
        {

        }
        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        public String GreenlistLocation()
        {
            string json = "greenlist.json";

            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;

            string jsonFile = Path.Combine(appRoot, "Datasource", json);

            if (File.Exists(jsonFile))
                return new FileInfo(jsonFile).FullName;
            else
                return null;
        }

        public String DroidCsvOutputLocation()
        {
            var directory = new DirectoryInfo(TargetFolder);
            var files = directory.GetFiles("*.csv");

            if (files.Count() > 0)
            { 
                FileInfo droidCsvFile = files.OrderByDescending(item => item.CreationTime).First();
                if (droidCsvFile == null)
                    return null;
                else
                    return droidCsvFile.FullName;
            }
            else
            {
                return null;
            }
        }

        public override void Execute()
        {            
            try
            {
                string droidCsvFile = DroidCsvOutputLocation();
                if (String.IsNullOrEmpty(droidCsvFile))
                    return;

                string greenListLocation = GreenlistLocation();
                if (String.IsNullOrEmpty(greenListLocation))
                    return;

                string extensionJson = File.ReadAllText(greenListLocation);
                var extensionData = JsonConvert.DeserializeObject<GreenListItem[]>(extensionJson);

                using (var reader = new StreamReader(droidCsvFile))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>();
                        var filesByDroid = records.Where(item
                            => item.TYPE == "File" && item.EXT != "metadata").Select(item => new
                            {
                                SessionId = SessionGuid,
                                Location = item.FILE_PATH,
                                Name = item.NAME,
                                Extension = item.EXT,
                                FormatName = item.FORMAT_NAME,
                                FormatVersion = item.FORMAT_VERSION,
                                Puid = item.PUID
                            }).ToList();

                        var doesContainsGreenListResult = filesByDroid.Where(item
                            => extensionData.Select(green
                                => green.Extension.ToUpper()).ToList().Contains(item.Extension.ToUpper())).ToList();

                        var doNotcontainsGreenListResult = filesByDroid.Except(doesContainsGreenListResult).ToList();

                        var endResult = doesContainsGreenListResult.Select(item
                            => new { item, InGreenList = true }).Concat(doNotcontainsGreenListResult.Select(item
                                => new { item, InGreenList = false })).ToList();

                        if (endResult.Count == 0)
                        {                            
                            var process = new ProcessResult(SessionGuid)
                            {
                                CollectionItem = TargetFolder,
                                Code = "Greenlist",
                                CreationTimestamp = DateTime.Now,
                                ActionName = this.GetType().Name,
                                Message = "Geen resultaten."
                            };
                            SaveJson(new DirectoryInfo(TargetFolder), this, process);                    
                        }
                        else
                        {
                            SaveJson(new DirectoryInfo(TargetFolder), this, endResult.ToArray());
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Logger.LogError(e, "Determine Droid CSV with greenlist failed!");
            }
        }
    }
}