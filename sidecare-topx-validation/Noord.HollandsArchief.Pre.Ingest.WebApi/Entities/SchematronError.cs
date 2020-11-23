using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{    public class Error
    {
        [JsonProperty("failed-assert-test")]
        public string FailedAssertTest { get; set; }
        [JsonProperty("fired-rule-context")]
        public string FiredRuleContext { get; set; }
        [JsonProperty("failed-assert-location")]
        public string FailedAssertLocation { get; set; }
        public string message { get; set; }
    }

    public class SchematronValidationReport
    {
        public List<Error> errors { get; set; }
    }

    public class Root
    {
        [JsonProperty("schematron-validation-report")]
        public SchematronValidationReport SchematronValidationReport { get; set; }
    }
}
