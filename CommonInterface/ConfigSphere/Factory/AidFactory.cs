using IDTechSDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigSphere.Factory
{
    [Serializable]
    public class AidFactory
    {
        private byte[][] Names = 
        {
            Common.getByteArray("A0000000031010"),
            Common.getByteArray("A0000000032010"),
            Common.getByteArray("A0000000041010"),
            Common.getByteArray("A00000002501"),
            Common.getByteArray("A0000001523010"),
            Common.getByteArray("A0000001524010")
        };

        private byte[][] Aids =
        {
            Common.getByteArray("9F01065649534130305F5701005F2A0208409F090200965F3601029F1B0400003A98DF25039F3704DF28039F0802DFEE150101DF13050000000000DF14050000000000DF15050000000000DF180100DF170400002710DF190100"),
            Common.getByteArray("9F01065649534130305F5701005F2A0208409F090200965F3601029F1B0400003A98DF25039F3704DF28039F0802DFEE150101DF13050000000000DF14050000000000DF15050000000000DF180100DF170400002710DF190100"),
            Common.getByteArray("5F5701005F2A0208409F090200025F3601029F1B0400003A98DF25039F3704DF28039F0802DFEE150101DF13050000000000DF14050000000000DF15050000000000DF180100DF170400002710DF190100"),
            Common.getByteArray("5F5701005F2A0208409F090200015F3601029F1B0400003A98DF25039F3704DF28039F0802DFEE150101DF13050000000000DF14050000000000DF15050000000000DF180100DF170400002710DF190100"),
            Common.getByteArray("5F5701005F2A0208409F090200015F3601029F1B0400003A98DF25039F3704DF28039F0802DFEE150101DF13050000000000DF14050000000000DF15050000000000DF180100DF170400002710DF190100"),
            Common.getByteArray("5F5701005F2A0208409F090200015F3601029F1B0400003A98DF25039F3704DF28039F0802DFEE150101DF13050000000000DF14050000000000DF15050000000000DF180100DF170400002710DF190100"),
        };

        public Dictionary<byte [], byte []> aid { get; set; }

        public AidFactory()
        {
            aid = new Dictionary<byte[], byte[]>();
            var dict = Names.Zip(Aids, (x, y) => new { x, y }).ToDictionary(val => val.x, val => val.y);
            foreach (var element in dict)
            {
                aid.Add(element.Key, element.Value);
            }
        }

        public Dictionary<byte[], byte[]> GetFactoryAids()
        {
            return aid;
        }

    }
}
