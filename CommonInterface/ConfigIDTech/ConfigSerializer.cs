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
using IPA.CommonInterface.ConfigIDTech.Factory;
using IPA.CommonInterface.ConfigIDTech.Configuration;

namespace IPA.CommonInterface.ConfigIDTech
{
    [Serializable]
    public class ConfigSerializer
    {
        /********************************************************************************************************/
        // ATTRIBUTES
        /********************************************************************************************************/
        #region -- attributes --
        private const string JSON_CONFIG = "configuration.json";
        private const string TERMINAL_CONFIG = "TerminalData";

        public TerminalConfiguration terminalCfg;

        private string fileName;
        #endregion

        public void ReadConfig()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                string path = System.IO.Directory.GetCurrentDirectory(); 
                fileName = path + "\\" + JSON_CONFIG;

                if(File.Exists(fileName))
                {
                    string FILE_CFG = File.ReadAllText(fileName);
                    terminalCfg = JsonConvert.DeserializeObject<TerminalConfiguration>(FILE_CFG);
                }
                else
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var resourceName = "IPA.CommonInterface.Assets." + JSON_CONFIG;
                    string [] resources = this.GetType().Assembly.GetManifestResourceNames();

                    string resourceContent = "";

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if(stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                resourceContent = reader.ReadToEnd();
                                terminalCfg = JsonConvert.DeserializeObject<TerminalConfiguration>(resourceContent);
                            }
                        }
                    }
                }

                if(terminalCfg != null)
                {
                    // config_meta
                    Debug.WriteLine("config_meta: type --------------  =[{0}]", (object) terminalCfg.config_meta.Type);
                    Debug.WriteLine("config_meta: production --------- =[{0}]", terminalCfg.config_meta.Production);
                    Debug.WriteLine("config_meta: Customer->Company -- =[{0}]", (object) terminalCfg.config_meta.Customer.Company);
                    Debug.WriteLine("config_meta: Customer->Contact -- =[{0}]", (object) terminalCfg.config_meta.Customer.Contact);
                    Debug.WriteLine("config_meta: Customer->Id ------- =[{0}]", terminalCfg.config_meta.Customer.Id);
                    Debug.WriteLine("config_meta: Id ----------------- =[{0}]", terminalCfg.config_meta.Id);
                    Debug.WriteLine("config_meta: Notes -------------- =[{0}]", (object) terminalCfg.config_meta.Notes);
                    Debug.WriteLine("config_meta: Version ------------ =[{0}]", (object) terminalCfg.config_meta.Version);
                    Debug.WriteLine("config_meta: terminal_type ------ =[{0}]", (object) terminalCfg.config_meta.Terminal_type);
                    // hardware
                    Debug.WriteLine("hardware   : serial_num --------- =[{0}]", (object) terminalCfg.hardware.Serial_num);
                    Debug.WriteLine("hardware   : contactless_available=[{0}]", (object) terminalCfg.hardware.Contactless_available);
                    // general_configuration
                    //Contact
                    //Msr_settings
                    //Terminal_info
                    Debug.WriteLine("general_configuration: TI->FWVR --------- =[{0}]", (object) terminalCfg.general_configuration.Terminal_info.firmware_ver);
                    Debug.WriteLine("general_configuration: TI->KVER --------- =[{0}]", (object) terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_ver);
                    Debug.WriteLine("general_configuration: TI->KCHK --------- =[{0}]", (object) terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_checksum);
                    Debug.WriteLine("general_configuration: TI->KCFG --------- =[{0}]", (object) terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_configuration_checksum);
                    //Encryption
                    Debug.WriteLine("general_configuration: EN->TYPE --------- =[{0}]", (object) terminalCfg.general_configuration.Encryption.data_encryption_type);
                    Debug.WriteLine("general_configuration: EN->MSRE --------- =[{0}]", (object) terminalCfg.general_configuration.Encryption.msr_encryption_enabled);
                    Debug.WriteLine("general_configuration: EN->ICCE --------- =[{0}]", (object) terminalCfg.general_configuration.Encryption.icc_encryption_enabled);
                    // user_configuration
                    Debug.WriteLine("user_configuration: expiration_masking -- =[{0}]", (object) terminalCfg.user_configuration.expiration_masking);
                    Debug.WriteLine("user_configuration: pan_clear_digits ---- =[{0}]", (object) terminalCfg.user_configuration.pan_clear_digits);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK1  =[{0}]", (object) terminalCfg.user_configuration.swipe_force_mask.track1);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK2  =[{0}]", (object) terminalCfg.user_configuration.swipe_force_mask.track2);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK3  =[{0}]", (object) terminalCfg.user_configuration.swipe_force_mask.track3);
                    Debug.WriteLine("user_configuration: swipe_force_mask:TK0  =[{0}]", (object) terminalCfg.user_configuration.swipe_force_mask.track3card0);
                    Debug.WriteLine("user_configuration: swipe_mask:TK1 ------ =[{0}]", (object) terminalCfg.user_configuration.swipe_mask.track1);
                    Debug.WriteLine("user_configuration: swipe_mask:TK2 ------ =[{0}]", (object) terminalCfg.user_configuration.swipe_mask.track2);
                    Debug.WriteLine("user_configuration: swipe_mask:TK3 ------ =[{0}]", (object) terminalCfg.user_configuration.swipe_mask.track3);
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
                if(terminalCfg != null)
                {
                    // Update timestamp
                    DateTime timenow = DateTime.UtcNow;
                    terminalCfg.user_configuration.last_update_timestamp = JsonConvert.SerializeObject(timenow).Trim('"');
                    Debug.WriteLine(terminalCfg.user_configuration.last_update_timestamp);

                    JsonSerializer serializer = new JsonSerializer();
                    string path = System.IO.Directory.GetCurrentDirectory(); 
                    fileName = path + "\\" + JSON_CONFIG;

                    using (StreamWriter sw = new StreamWriter(fileName))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                       serializer.Formatting = Formatting.Indented;
                       serializer.Serialize(writer, terminalCfg);
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

                    cfgTerminalDataMetaObject.general_configuration = terminalCfg.general_configuration;

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
}
