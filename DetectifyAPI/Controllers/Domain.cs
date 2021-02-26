using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DetectifyAPI.Controllers
{
    internal class Domain
    {
        [JsonPropertyName("name")]
        public string Address { get; internal set; }
        [JsonPropertyName("ip_address")]
        public List<string> IP { get; internal set; }

        public Domain()
        {
            IP = new List<string>();
        }
    }
}