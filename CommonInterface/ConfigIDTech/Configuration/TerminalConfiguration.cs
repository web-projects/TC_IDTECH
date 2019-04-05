using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace IPA.CommonInterface.ConfigIDTech.Configuration
{
    [Serializable]
    public class TerminalConfiguration
    {
        [JsonProperty(PropertyName = "config_meta", Order = 1)]
        public config_meta config_meta { get; set; }

        [JsonProperty(PropertyName = "hardware", Order = 2)]
        public hardware hardware;

        [JsonProperty(PropertyName = "general_configuration", Order = 3)]
        public general_configuration general_configuration;

        [JsonProperty(PropertyName = "user_configuration", Order = 4)]
        public user_configuration user_configuration;
    }

    [Serializable]
    public class ConfigRoot
    {
        [JsonProperty(PropertyName = "config_meta", Order = 1)]
        public config_meta config_meta;
        [JsonProperty(PropertyName = "hardware", Order = 2)]
        public hardware    hardware;
    }

    [Serializable]
    public class config_meta
    {
        public string Type { get; set; }
        public bool Production { get; set; }
        public Customer Customer { get; set; }
        public int Id { get; set; }
        public string Notes { get; set; }
        public string Version { get; set; }
        public string Terminal_type { get; set; }
    }

    [Serializable]
    public class Customer
    {
      public string Company { get; set; }
      public string Contact { get; set; }
      public int    Id { get; set; }
    }

    [Serializable]
    public class hardware
    {
       public string  Serial_num { get; set; }
       public string Contactless_available { get; set; }
    }

    [Serializable]
    public class general_configuration
    {
        public Contact Contact { get; set; }
        public List<MSRSettings> msr_settings { get; set; }
        public TerminalInfo Terminal_info { get; set; }
        public Encryption Encryption { get; set; }
    }

    [Serializable]
    public class Contact
    {
      public List<Capk> capk{ get; set; }
      public List<Aid> aid { get; set; }
      public string terminal_ics_type { get; set; }
      public string terminal_data { get; set; }
      [JsonExtensionData]
      public Dictionary<string, object> tags { get; set; }
    }

    [Serializable]
    public class Encryption
    {
      public string data_encryption_type { get; set; }
      public bool msr_encryption_enabled { get; set; }
      public bool icc_encryption_enabled { get; set; }
      //"crl": {}
    }
    
    [Serializable]
    public class user_configuration
    {
        public bool expiration_masking { get; set; }
        public int pan_clear_digits { get; set; }
        public swipe_force_mask swipe_force_mask  { get; set; }
        public swipe_mask swipe_mask  { get; set; }
        public string last_update_timestamp  { get; set; }
    }  

   [Serializable]
   public class swipe_force_mask
   {
        public bool track1 { get; set; }
        public bool track2 { get; set; }
        public bool track3 { get; set; }
        public bool track3card0 { get; set; }
   }

    [Serializable]
    public class swipe_mask
    {
        public bool track1 { get; set; }
        public bool track2 { get; set; }
        public bool track3 { get; set; }
    }

     [Serializable]
    public class TerminalDataConfigSerializer
    {
        [JsonProperty(PropertyName = "general_configuration", Order = 1)]
        public general_configuration general_configuration;
        [JsonProperty(PropertyName = "last_update_timestamp", Order = 2)]
        public string last_update_timestamp  { get; set; }
    }
}
