using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.DAL.RBADAL.Services.Devices.IDTech.Models
{
    public class TrackData
    {
        public bool IsSwipe { get; set; }

        public bool IsDebit { get; set; }
        public bool IsEmv { get; set; }
        public bool IsSignatureRequired { get; set; }
        public string T1Data { get; set; }
        public string T2Data { get; set; }
        public string T3Data { get; set; }

        public string T1Crypto { get; set; }
        public string T2Crypto { get; set; }
        public string T3Crypto { get; set; }

        public string T1Hash { get; set; }
        public string T2Hash { get; set; }
        public string T3Hash { get; set; }

        public string SerialNumber { get; set; }
        public string Ksn { get; set; }

        public byte[] DeviceData { get; set; }

        public string PAN { get; set; }
        public string Name { get; set; }
        public string ExpDate { get; set; }
        public string Addr { get; set; }
        public string Zip { get; set; }

        #region extension methods 
        public string Track1 { get { return T1Data; } }
        public string Track2 { get { return T2Data; } }
        public string Track3 { get { return T1Crypto + T2Crypto + T3Crypto + T1Hash + T2Hash + T3Hash + SerialNumber + Ksn; } }
        //public string EncryptedTracks { get { return ByteArrayToHexString(SubArray<byte>(report.Data, osStx, osT1Data)) + t1Data + t2Data + t3Data + ByteArrayToHexString(SubArray<byte>(report.Data, osT1Crypto, osEtx - osT1Crypto + 1)); } }  //todo: expose pointers
        public string EncryptedTracks { get; set; }

        #endregion

    }

    //internal class Track1
    public  class Track1
    {
        public string PAN { get; set; }
        public string Name { get; set; }
        public string ExpDate { get; set; }
    }

    //internal class Track3
    public class Track3
    {
        string Address { get; set; }
        string ZIP { get; set; }
    }

    #region IDTechM1XX_XML_Format

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class DvcMsg
    {

        private DvcMsgDvc dvcField;

        private DvcMsgCard cardField;

        private object addrField;

        private DvcMsgTran tranField;

        private decimal verField;

        /// <remarks/>
        public DvcMsgDvc Dvc
        {
            get
            {
                return this.dvcField;
            }
            set
            {
                this.dvcField = value;
            }
        }

        /// <remarks/>
        public DvcMsgCard Card
        {
            get
            {
                return this.cardField;
            }
            set
            {
                this.cardField = value;
            }
        }

        /// <remarks/>
        public object Addr
        {
            get
            {
                return this.addrField;
            }
            set
            {
                this.addrField = value;
            }
        }

        /// <remarks/>
        public DvcMsgTran Tran
        {
            get
            {
                return this.tranField;
            }
            set
            {
                this.tranField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal Ver
        {
            get
            {
                return this.verField;
            }
            set
            {
                this.verField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class DvcMsgDvc
    {

        private string appField;

        private decimal appVerField;

        private string dvcTypeField;

        private ulong dvcSNField;

        private string entryField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string App
        {
            get
            {
                return this.appField;
            }
            set
            {
                this.appField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal AppVer
        {
            get
            {
                return this.appVerField;
            }
            set
            {
                this.appVerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DvcType
        {
            get
            {
                return this.dvcTypeField;
            }
            set
            {
                this.dvcTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ulong DvcSN
        {
            get
            {
                return this.dvcSNField;
            }
            set
            {
                this.dvcSNField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Entry
        {
            get
            {
                return this.entryField;
            }
            set
            {
                this.entryField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class DvcMsgCard
    {

        private byte cEncodeField;

        private string eTrk1Field;

        private string eTrk2Field;

        private string cDataKSNField;

        private ushort expField;

        private string mskPANField;

        private string cHolderField;

        private byte eFormatField;

        private string eCDataField;


        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte CEncode
        {
            get
            {
                return this.cEncodeField;
            }
            set
            {
                this.cEncodeField = value;
            }
        }
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ECData
        {
            get
            {
                return this.eCDataField;
            }
            set
            {
                this.eCDataField = value;
            }
        }
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ETrk1
        {
            get
            {
                return this.eTrk1Field;
            }
            set
            {
                this.eTrk1Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ETrk2
        {
            get
            {
                return this.eTrk2Field;
            }
            set
            {
                this.eTrk2Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string CDataKSN
        {
            get
            {
                return this.cDataKSNField;
            }
            set
            {
                this.cDataKSNField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort Exp
        {
            get
            {
                return this.expField;
            }
            set
            {
                this.expField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MskPAN
        {
            get
            {
                return this.mskPANField;
            }
            set
            {
                this.mskPANField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string CHolder
        {
            get
            {
                return this.cHolderField;
            }
            set
            {
                this.cHolderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte EFormat
        {
            get
            {
                return this.eFormatField;
            }
            set
            {
                this.eFormatField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class DvcMsgTran
    {

        private string tranTypeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string TranType
        {
            get
            {
                return this.tranTypeField;
            }
            set
            {
                this.tranTypeField = value;
            }
        }
    }

    #endregion
}
