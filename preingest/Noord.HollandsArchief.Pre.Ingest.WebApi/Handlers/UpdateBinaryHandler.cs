using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Structure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{
    //Check 5
    public class UpdateBinaryHandler : AbstractPreingestHandler
    {
        AppSettings _appSettings = null;

        public UpdateBinaryHandler(AppSettings settings) : base(settings)
        {
            _appSettings = settings;
        }

        private String TargetFolder { get => Path.Combine(ApplicationSettings.DataFolderName, SessionGuid.ToString()); }

        public override void Execute()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(TargetFolder);
                var jsonFiles = directoryInfo.GetFiles("*.json");

                var fileinfo = directoryInfo.GetFiles("SidecarValidationHandler*.bin").FirstOrDefault();

                if (fileinfo == null)
                    throw new FileNotFoundException("No binary file found! Cannot update. Run sidecar validation first.");

                PairNode<ISidecar> sidecar = Utilities.DeserializerHelper.DeSerializeObjectFromBinaryFile<PairNode<ISidecar>>(fileinfo.FullName);

                var droidCsvFile = directoryInfo.GetFiles("*.csv").OrderByDescending(item => item.CreationTime).FirstOrDefault();
                if (droidCsvFile == null)
                    throw new FileNotFoundException("Droid CSV file not found! Cannot update. Run droid profile and export CSV first.");

                var metadataValidation = directoryInfo.GetFiles("MetadataValidationHandler*.json").OrderByDescending(item => item.CreationTime).FirstOrDefault();
                if (metadataValidation == null)
                    throw new FileNotFoundException("MetadataValidationHandler.json file not found! Cannot update. Run metadata validation check first.");

                var metadataValidationResultList = JsonConvert.DeserializeObject<List<ProcessResult>>(System.IO.File.ReadAllText(metadataValidation.FullName));
                if (metadataValidationResultList == null)
                    throw new ApplicationException("Deserialize JSON object failed!");

                string greenListLocation = GreenlistLocation();
                if (String.IsNullOrEmpty(greenListLocation))
                    return;

                string extensionJson = File.ReadAllText(greenListLocation);
                var greenListData = JsonConvert.DeserializeObject<GreenListItem[]>(extensionJson).ToList();

                using (var reader = new StreamReader(droidCsvFile.FullName))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<dynamic>().Select(item => new PronomItem
                        {
                            Id = item.ID,
                            ParentId = item.PARENT_ID,
                            Uri = item.URI,
                            FilePath = item.FILE_PATH,
                            Name = item.NAME,
                            Method = item.METHOD,
                            Status = item.STATUS,
                            Size = item.SIZE,
                            Type = item.TYPE,
                            Ext = item.EXT,
                            LastModified = item.LAST_MODIFIED,
                            ExtensionMisMatch = item.EXTENSION_MISMATCH,
                            Hash = item.HASH,
                            FormatCount = item.FORMAT_COUNT,
                            Puid = item.PUID,
                            MimeType = item.MIME_TYPE,
                            FormatName = item.FORMAT_NAME,
                            FormatVersion = item.FORMAT_VERSION
                        }).ToList();

                        sidecar.Traverse(new PairNode<ISidecar>.TraversalDataDelegate((data) =>
                        {
                            bool result = false;
                            try
                            {
                                //add pronom
                                data.PronomMetadataInfo = records.FirstOrDefault(item => item.FilePath == data.MetadataFileLocation);
                                if (data is Bestand)
                                {
                                    (data as Bestand).PronomBinaryInfo = records.FirstOrDefault(item => item.FilePath.Equals( (data as Bestand).BinaryFileLocation, StringComparison.InvariantCultureIgnoreCase));
                                    //add greenlist
                                    if ((data as Bestand).PronomBinaryInfo != null)
                                        (data as Bestand).BinaryFileIsInGreenList = greenListData == null ? false : greenListData.Exists(item => item.Extension.Equals((data as Bestand).PronomBinaryInfo.Ext, StringComparison.InvariantCultureIgnoreCase));                                   
                                }
                                //add validation
                                var validationFailedMessages = metadataValidationResultList.Where(item => item.CollectionItem == data.MetadataFileLocation).SelectMany(item => item.Messages).Select(item => new SidecarException(item));
                                (data as Sidecar).ObjectExceptions().AddRange(validationFailedMessages);
                                
                                result = true;
                            }
                            catch { result = false; }
                            return result;
                        }));
                    }
                }
                SaveBinary(new DirectoryInfo(TargetFolder), "SidecarValidationHandler", sidecar);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {

            }
        }
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
    }
}
