using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigSphere.Configuration
{
    [Serializable]
    public class TerminalInfo
    {
        [JsonProperty(PropertyName = "firmware_ver", Order = 1)]
        public string firmware_ver { get; set; }
        [JsonProperty(PropertyName = "contact_emv_kernel_ver", Order = 2)]
        public string contact_emv_kernel_ver { get; set; }
        [JsonProperty(PropertyName = "contact_emv_kernel_checksum", Order = 3)]
        public string contact_emv_kernel_checksum { get; set; }
        [JsonProperty(PropertyName = "contact_emv_kernel_configuration_checksum", Order = 4)]
        public string contact_emv_kernel_configuration_checksum { get; set; }
    }
}
