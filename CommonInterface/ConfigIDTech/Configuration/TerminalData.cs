using IDTechSDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigIDTech.Configuration
{
    [Serializable]
    public class TerminalData
    {
        /*
            TAG 5F36 - TLV information for "Transaction Currency Exponent"
            TAG 9F1A - "Terminal Country Code"
            TAG 9F35 - "Terminal Type"
            TAG 9F33 - "Terminal Capabilities" 
            TAG 9F40 - "Additional Terminal Capabilities"
            TAG 9F1E - "Interface Device Serial Number"
            TAG 50   - "Tag Application Label"
            TAG 73   - "Directory Discretionary Template"
            TAG 86   - "Issuer Script Command" 
        */
        
        [JsonProperty(PropertyName = "terminal_data", Order = 1)]
        public string terminal_data { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> tags { get; set; }
        [JsonIgnore]
        private byte [] tlv;

        public TerminalData(byte [] param)
        {
            tlv = param;
            tags = new  Dictionary<string, object>();
        }

        public string ConvertTLVToString()
        {
            byte[] test = tlv.Skip(0).Take(tlv.Length).ToArray();
            terminal_data = BitConverter.ToString(test).Replace("-", string.Empty);
            return terminal_data;
        }

        public string ConvertTLVToValuePairs()
        {
            string text = "";
            try
            {
                var values = Common.processTLVUnencrypted(tlv);
                values = values.ToDictionary(kv => kv.Key, kv => (string)((string)kv.Value).ToUpper());
                Debug.WriteLine("====================== TERMINAL DATA : TLV DUMP ======================");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    tags.Add(kvp.Key, kvp.Value);
                    text += kvp.Key + ": " + kvp.Value + "\r\n";
                    Debug.WriteLine("{0} : {1}", kvp.Key , kvp.Value);
                }
                Debug.WriteLine("======================================================================");
            }
            catch(Exception exp)
            {
                Debug.WriteLine("TerminalDatga::ConvertTLVToValuePairs(): - exception={0}", (object)exp.Message);
            }
            return text;
        }

        public string [] ConvertTLVToValuePairsArray()
        {
            List<string> collection = new List<string>();
            try
            {
                var values = Common.processTLVUnencrypted(tlv);
                values = values.ToDictionary(kv => kv.Key, kv => (string)((string)kv.Value).ToUpper());
                Debug.WriteLine("====================== TERMINAL DATA : TLV DUMP ======================");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    collection.Add(string.Format("{0}:{1}", kvp.Key, kvp.Value).ToUpper());
                    Debug.WriteLine("{0} : {1}", kvp.Key , kvp.Value);
                }
                Debug.WriteLine("======================================================================");
            }
            catch(Exception exp)
            {
                Debug.WriteLine("TerminalDatga::ConvertTLVToValuePairs(): - exception={0}", (object)exp.Message);
            }
            string [] text = collection.ToArray();
            return text;
        }

        public string GetTagValue(string key)
        {
            object value = "";
            tags.TryGetValue(key, out value);
            return (string) value;
        }

        public Dictionary<string, object> GetTags()
        {
            return tags;
        }
    }

    [Serializable]
    public class TData : DynamicObject
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();

            // If the property name is found in a dictionary,
            // set the result parameter to the property value and return true.
            return dictionary.TryGetValue(name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            dictionary[binder.Name.ToLower()] = value;

            // You can always add a value to a dictionary,
            // so this method always returns true.
            return true;
        }
    }
}
