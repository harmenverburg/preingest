using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event;
using System.Collections;
using Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Handler;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Output
{
    public class SettingsReader
    {
        private PreingestActionModel _model = null;
        public SettingsReader(String dataFolder, Guid guid, String filename = "SettingsHandler.json")
        {
            String jsonFile = Path.Combine(dataFolder, guid.ToString(), filename);
            if (File.Exists(jsonFile))
            {
                string jsonContent = File.ReadAllText(jsonFile);
                _model = JsonConvert.DeserializeObject<PreingestActionModel>(jsonContent);
            }
        }

        public BodySettings GetSettings()
        {
            if (_model == null)
                return null;

            if (_model.ActionData == null)
                return null;

            BodySettings settings = JsonConvert.DeserializeObject<BodySettings>(_model.ActionData.ToString());
            return settings;
        }
    }
}
