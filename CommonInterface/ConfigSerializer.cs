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

namespace IPA.CommonInterface
{
    [Serializable]
    public class ConfigSerializer
    {
        private const string JSON_CONFIG = "configuration.json";
        private const string TERMINAL_CONFIG = "TerminalData";

        private ConfigSerializer cfgMetaObject;
        private string fileName;
        
        [JsonProperty(PropertyName = "config_meta", Order = 1)]
        public config_meta config_meta { get; set; }

        [JsonProperty(PropertyName = "hardware", Order = 2)]
        public hardware hardware;

        [JsonProperty(PropertyName = "general_configuration", Order = 3)]
        public general_configuration general_configuration;

        [JsonProperty(PropertyName = "user_configuration", Order = 4)]
        public user_configuration user_configuration;

        public void ReadConfig()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                string path = System.IO.Directory.GetCurrentDirectory(); 
                fileName = path + "\\" + JSON_CONFIG;
                string FILE_CFG = File.ReadAllText(fileName);
                //byte[] data = File.ReadAllBytes(fileName);
                //var FILE_CFG = Encoding.UTF8.GetString(data);

                cfgMetaObject = JsonConvert.DeserializeObject<ConfigSerializer>(FILE_CFG);

                if(cfgMetaObject != null)
                {
                    config_meta = cfgMetaObject.config_meta;
                    hardware    = cfgMetaObject.hardware;
                    general_configuration = cfgMetaObject.general_configuration;
                    user_configuration = cfgMetaObject.user_configuration;

                    // config_meta
                    Debug.WriteLine("config_meta: type --------------  =[{0}]", (object) config_meta.Type);
                    Debug.WriteLine("config_meta: production --------- =[{0}]", config_meta.Production);
                    Debug.WriteLine("config_meta: Customer->Company -- =[{0}]", (object) config_meta.Customer.Company);
                    Debug.WriteLine("config_meta: Customer->Contact -- =[{0}]", (object) config_meta.Customer.Contact);
                    Debug.WriteLine("config_meta: Customer->Id ------- =[{0}]", config_meta.Customer.Id);
                    Debug.WriteLine("config_meta: Id ----------------- =[{0}]", config_meta.Id);
                    Debug.WriteLine("config_meta: Notes -------------- =[{0}]", (object) config_meta.Notes);
                    Debug.WriteLine("config_meta: Version ------------ =[{0}]", (object) config_meta.Version);
                    Debug.WriteLine("config_meta: terminal_type ------ =[{0}]", (object) config_meta.Terminal_type);
                    // hardware
                    Debug.WriteLine("hardware   : serial_num --------- =[{0}]", (object) hardware.Serial_num);
                    Debug.WriteLine("hardware   : contactless_available=[{0}]", (object) hardware.Contactless_available);
                    // general_configuration
                    //Contact
                    //Msr_settings
                    //Terminal_info
                    Debug.WriteLine("general_configuration: TI->FWVR --------- =[{0}]", (object) general_configuration.Terminal_info.firmware_ver);
                    Debug.WriteLine("general_configuration: TI->KVER --------- =[{0}]", (object) general_configuration.Terminal_info.contact_emv_kernel_ver);
                    Debug.WriteLine("general_configuration: TI->KCHK --------- =[{0}]", (object) general_configuration.Terminal_info.contact_emv_kernel_checksum);
                    Debug.WriteLine("general_configuration: TI->KCFG --------- =[{0}]", (object) general_configuration.Terminal_info.contact_emv_kernel_configuration_checksum);
                    //Encryption
                    Debug.WriteLine("general_configuration: EN->TYPE --------- =[{0}]", (object) general_configuration.Encryption.data_encryption_type);
                    Debug.WriteLine("general_configuration: EN->MSRE --------- =[{0}]", (object) general_configuration.Encryption.msr_encryption_enabled);
                    Debug.WriteLine("general_configuration: EN->ICCE --------- =[{0}]", (object) general_configuration.Encryption.icc_encryption_enabled);
                    // user_configuration
                    Debug.WriteLine("user_configuration: expiration_masking -- =[{0}]", (object) user_configuration.expiration_masking);
                    Debug.WriteLine("user_configuration: pan_clear_digits ---- =[{0}]", (object) user_configuration.pan_clear_digits);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK1  =[{0}]", (object) user_configuration.swipe_force_mask.track1);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK2  =[{0}]", (object) user_configuration.swipe_force_mask.track2);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK3  =[{0}]", (object) user_configuration.swipe_force_mask.track3);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK0  =[{0}]", (object) user_configuration.swipe_force_mask.track3card0);
                    Debug.WriteLine("user_configuration: swipe_mask:TK1 ------ =[{0}]", (object) user_configuration.swipe_mask.track1);
                    Debug.WriteLine("user_configuration: swipe_mask:TK2 ------ =[{0}]", (object) user_configuration.swipe_mask.track2);
                    Debug.WriteLine("user_configuration: swipe_mask:TK3 ------ =[{0}]", (object) user_configuration.swipe_mask.track3);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("JsonSerializer: exception: {0}", (object) ex.Message);
            }
        }

        public void WriteConfig()
        {
            try
            {
                if(cfgMetaObject != null)
                {
                    // Update timestamp
                    DateTime timenow = DateTime.UtcNow;
                    user_configuration.last_update_timestamp = JsonConvert.SerializeObject(timenow).Trim('"');
                    Debug.WriteLine(user_configuration.last_update_timestamp);

                    //cfgMetaObject.config_meta = config_meta;
                    //cfgMetaObject.hardware = hardware;
                    //cfgMetaObject.general_configuration = general_configuration;
                    cfgMetaObject.user_configuration = user_configuration;

                    JsonSerializer serializer = new JsonSerializer();
                    string path = System.IO.Directory.GetCurrentDirectory(); 
                    fileName = path + "\\" + JSON_CONFIG;

                    using (StreamWriter sw = new StreamWriter(fileName))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                       serializer.Formatting = Formatting.Indented;
                       serializer.Serialize(writer, cfgMetaObject);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("JsonSerializer: exception: {0}", ex);
            }
        }

        public void WriteTerminalDataConfig()
        {
            try
            {
                TerminalDataConfigSerializer cfgTerminalDataMetaObject =  JsonConvert.DeserializeObject<TerminalDataConfigSerializer>("{\"general_configuration\": {\"Contact\": {\"terminal_ics_type\": \"\",\"terminal_data\": \"\"}}}");

                if(cfgTerminalDataMetaObject != null)
                {
                    // Update timestamp
                    DateTime timenow = DateTime.UtcNow;
                    cfgTerminalDataMetaObject.last_update_timestamp = JsonConvert.SerializeObject(timenow).Trim('"');
                    Debug.WriteLine(cfgTerminalDataMetaObject.last_update_timestamp);

                    cfgTerminalDataMetaObject.general_configuration = general_configuration;

                    JsonSerializer serializer = new JsonSerializer();
                    string path = System.IO.Directory.GetCurrentDirectory(); 
                    string extension = timenow.ToString("yyyyMMddhhmm").Trim('"');
                    fileName = path + "\\" + TERMINAL_CONFIG + "." + extension + ".json";

                    using (StreamWriter sw = new StreamWriter(fileName))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                       serializer.Formatting = Formatting.Indented;
                       serializer.Serialize(writer, cfgTerminalDataMetaObject);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("JsonSerializer: exception: {0}", ex);
            }
        }

        public string GetFileName()
        {
            return fileName;
        }
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
