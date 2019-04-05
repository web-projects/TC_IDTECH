using IDTechSDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigIDTech.Configuration
{
    [Serializable]
    public class Aid
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        string name { get; set; }
        [JsonProperty(PropertyName = "value", Order = 2)]
        string value { get; set; }
        [JsonIgnore]
        private byte [] data;

        public Aid(byte[] _name, byte[] _value)
        {
            if(_name != null)
            {
                this.name = BitConverter.ToString(_name).Replace("-", string.Empty);
            }

            if(_value != null)
            {
                this.data  = _value;
                this.value =  BitConverter.ToString(_value).Replace("-", string.Empty);
            }
        }

        public string ConvertTLVToValuePairs()
        {
            string text = "";
            try
            {
                Dictionary<string, string> dict = Common.processTLVUnencrypted(data);
                Debug.WriteLine("==================== TLV DUMP ====================");
                Debug.WriteLine("AID  : {0}", (object) this.name);
                foreach (KeyValuePair<string, string> kvp in dict)
                {
                    text += kvp.Key + ": " + kvp.Value + "\r\n";
                    Debug.WriteLine("{0} : {1}", kvp.Key , kvp.Value);
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("Aid::ConvertTLVToValuePairs(): - exception={0}", (object)exp.Message);
            }
            return text;
        }
    }
}
