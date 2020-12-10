using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Handlers
{   public class SpreadSheetHandler
    {
        private AppSettings _appSettings = null;

        private IDictionary<String, String> SheetNameMapping(Guid guid)
        {
            Dictionary<String, String> sheetMappingNames = new Dictionary<string, string>();
            sheetMappingNames.Add("Uitpakken", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "UnpackTarHandler.json"));
            sheetMappingNames.Add("Virusscan", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "ScanVirusValidationHandler.json"));
            sheetMappingNames.Add("Ongeldige karakters", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "NamingValidationHandler.json"));
            sheetMappingNames.Add("Schema metadata", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "MetadataValidationHandler.json"));
            sheetMappingNames.Add("Sidecar structuur 0", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler.bin"));
            sheetMappingNames.Add("Sidecar structuur 1", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Archief.json"));
            sheetMappingNames.Add("Sidecar structuur 2", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Series.json"));
            sheetMappingNames.Add("Sidecar structuur 3", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Dossier.json"));
            sheetMappingNames.Add("Sidecar structuur 4", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Record.json"));
            sheetMappingNames.Add("Sidecar structuur 5", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Bestand.json"));
            sheetMappingNames.Add("Sidecar structuur 6", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Onbekend.json"));
            sheetMappingNames.Add("Sidecar structuur 7", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "SidecarValidationHandler_Samenvatting.json"));
            sheetMappingNames.Add("Droid eigenschappen 1", Path.Combine(_appSettings.DataFolderName, guid.ToString(), String.Format("{0}.droid.csv", guid)));
            sheetMappingNames.Add("Droid eigenschappen 2", Path.Combine(_appSettings.DataFolderName, guid.ToString(), String.Format("{0}.droid.xml", guid)));
            sheetMappingNames.Add("Droid eigenschappen 3", Path.Combine(_appSettings.DataFolderName, guid.ToString(), String.Format("{0}.planets.xml", guid)));
            sheetMappingNames.Add("Groene lijst", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "GreenListHandler.json"));
            sheetMappingNames.Add("Encoding metadata", Path.Combine(_appSettings.DataFolderName, guid.ToString(), "EncodingHandler.json"));
            return sheetMappingNames;
        }

        public SpreadSheetHandler(AppSettings settings)
        {
            _appSettings = settings;
        }
                

        public void CreateSpreadSheet(Guid guid)
        {
            string filePath = Path.Combine(_appSettings.DataFolderName, guid.ToString(), String.Format("{0}.xlsx", guid));
            // Create a spreadsheet document by supplying the filepath.
            // By default, AutoSave = true, Editable = true, and Type = xlsx.
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.
                Create(filePath, SpreadsheetDocumentType.Workbook);

            // Add a WorkbookPart to the document.
            WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            // Add a WorksheetPart to the WorkbookPart.
            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook.
            Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.
                AppendChild<Sheets>(new Sheets());

            string currenSessionFolder = Path.Combine(_appSettings.DataFolderName, guid.ToString());
            string[] files = Directory.GetFiles(currenSessionFolder);

            var mapping = this.SheetNameMapping(guid).Values.ToArray();

            var currentResult = mapping.Intersect(files).ToList();

            uint i = 1;
            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                //bool mapping.Contains(info.Name);
                
                // Append a new worksheet and associate it with the workbook.
                Sheet sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = i,
                    Name = info.Name.Replace(info.Extension, "")
                }; 
                sheets.Append(sheet);
                i++;
            }           

            workbookpart.Workbook.Save();
            // Close the document.
            spreadsheetDocument.Close();
        }
    }
}
