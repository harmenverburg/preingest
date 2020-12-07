using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities
{
    public class JsTreeItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("parent")]
        public string Parent { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }        
        [JsonProperty("icon")]
        public string Icon { get; set; }
        [JsonProperty("state")]
        public JsTreeState State{ get; set; }
        [JsonProperty("li_attr")]
        public string[] LiAttr { get; set; }
        [JsonProperty("a_attr")]
        public string[] AnchorAttr { get; set; }
        
    }

    public class JsTreeState
    {
        [JsonProperty("opened")]
        public bool Opened { get; set; }
        [JsonProperty("disabled")]
        public bool Disabled { get; set; }
        [JsonProperty("selected")]
        public bool Selected { get; set; }
    }


}
