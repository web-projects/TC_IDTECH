using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigSphere.Configuration
{
    [Serializable]
    public class Capk
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        string name { get; set; }
        [JsonProperty(PropertyName = "hash_algorithm", Order = 2)]
        string hash_algorithm { get; set; }
        [JsonProperty(PropertyName = "encryption_algorithm", Order = 3)]
        string encryption_algorithm { get; set; }
        [JsonProperty(PropertyName = "hash_value", Order = 4)]
        string hash_value { get; set; }
        [JsonProperty(PropertyName = "exponent", Order = 5)]
        string exponent { get; set; }
        [JsonProperty(PropertyName = "modulus_length", Order = 6)]
        string modulus_length { get; set; }
        [JsonProperty(PropertyName = "modulus", Order = 7)]
        string modulus { get; set; }

        [JsonIgnore]
        private byte[] capKValue;

        public Capk(byte[] key)
        {
            capKValue = key;
            if(key != null)
            {
                byte[] worker = key.Skip(0).Take(6).ToArray();
                name = BitConverter.ToString(worker).Replace("-", string.Empty);
                worker = key.Skip(6).Take(1).ToArray();
                hash_algorithm =  BitConverter.ToString(worker).Replace("-", string.Empty);
                worker = key.Skip(7).Take(1).ToArray();
                encryption_algorithm =  BitConverter.ToString(worker).Replace("-", string.Empty);
                worker =  key.Skip(8).Take(20).ToArray();
                hash_value = BitConverter.ToString(worker).Replace("-", string.Empty);
                worker = key.Skip(28).Take(4).ToArray();
                exponent =  BitConverter.ToString(worker).Replace("-", string.Empty);
                worker =  key.Skip(32).Take(2).ToArray();
                modulus_length = BitConverter.ToString(worker).Replace("-", string.Empty);
                worker = key.Skip(34).ToArray();
                modulus =  BitConverter.ToString(worker).Replace("-", string.Empty);
            }
        }

        public void ShowCapkValues()
        {
            Debug.WriteLine("capk: name: {0}", (object) name);
            Debug.WriteLine("capk: hash: {0}", (object) hash_algorithm);
            Debug.WriteLine("capk: algo: {0}", (object) encryption_algorithm);
            Debug.WriteLine("capk: valu: {0}", (object) hash_value );
            Debug.WriteLine("capk: expo: {0}", (object) exponent);
            Debug.WriteLine("capk: modl: {0}", (object) modulus_length);
            Debug.WriteLine("capk: modu: {0}", (object) modulus);
        }

        public string GetCapkName()
        {
            return name;
        }

        public byte [] GetCapkValue()
        {
            return capKValue;
        }

        public string GetModulus()
        {
            return modulus;
        }

        public string GetExponent()
        {
            return exponent;
        }
    }
}
