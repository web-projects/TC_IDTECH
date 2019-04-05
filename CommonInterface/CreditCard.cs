using IPA.Core.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPA.Core.Data.Entity.Other
{
    public class CreditCard
    {
        public string Track1 { get; set; }
        public string Track2 { get; set; }
        public string Track3 { get; set; }
        public string EncryptedTracks { get; set; }
        public string EMVTagsAuth { get; set; }
        public string EMVTagsAuthResponse { get; set; }
        public string EMVTagsConfirm { get; set; }
        public string EMVTagsConfirmResponse { get; set; }
        public string OriginalTCTransactionID { get; set; }
        public string AVSAddress1 { get; set; }
        public string AVSZip { get; set; }
        public string CreditCardNumber { get; set; }
        public string CardExpirationMonth { get; set; }
        public string CardExpirationYear { get; set; }
        public string CardHolderName { get; set; }
        public string CVV { get; set; }
        public bool EMVFallBack { get; set; }
        public bool SaveCard { get; set; }
        public bool RequestToken { get; set; }
        public string EncryptedPIN { get; set; }
        public string TransactionType { get; set; }
        public string EMVApplicationName { get; set; }
        public string EMVAuthorizationResponseCode { get; set; }
        public string EMVCardEntryMode { get; set; }
        public string EMVTransactionStatusInformation { get; set; }
        public DeviceAbortType AbortType { get; set; }
        public bool DebitCard { get; set; }

        public string ChipCard
        {
            get
            {
                if (EmvTagList.Where(t => t.Tag == "9f12").Select(t => t.Value).DefaultIfEmpty("").First() != "" &&
                    EmvTagList.Where(t => t.Tag == "9f11").Select(t => t.Value).DefaultIfEmpty("").First() == "01")
                    return EmvTagList.FirstOrDefault(t => t.Tag == "9f12").Value;

                else if (EmvTagList.Where(t => t.Tag == "50").Select(t => t.Value).DefaultIfEmpty("").First() != "")
                    return EmvTagList.FirstOrDefault(t => t.Tag == "50").Value;

                else return EmvTagList.Where(t => t.Tag == "4f").Select(t => t.Value).DefaultIfEmpty("").First();
            }
        }

        public IEnumerable<EmvTag> EmvTagList = new List<EmvTag>();

        public static IEnumerable<EmvTag> ParseEMVTags(string taglist, char delimiter)
        {
            var result = new List<EmvTag>();
            var list1 = taglist.Split(delimiter);

            try
            {
                foreach (string v in list1)
                {
                    if (v.StartsWith("emv_"))
                    {
                        var vals = v.Split('=');
                        var fields = v.Split('_');

                        result.Add(new EmvTag()
                        {
                            Tag = fields[1].Trim(),
                            Label = vals[0].Trim(),
                            Value = vals[1].Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Failure to get the description from an enumeration should not short circuit the caller.
                System.Diagnostics.Debug.WriteLine("ParseEMVTags() - Exception {0}", ex.Message);
            }
            return result;
        }

        public bool IsPinVerfied
        {
            get
            {
                const int VERIFIED_PLAIN_ICC = 1;
                const int VERIFIED_CYPHER_ONLINE = 2;
                const int VERIFIED_PLAIN_ICC_SIG = 3;
                const int VERIFIED_CYPHER_ICCG = 4;
                const int VERIFIED_CYPHER_ICCG_SIG = 5;

                if (EmvTagList.Where(t => t.Tag == "9f34").Select(t => t.Value).DefaultIfEmpty("").First() == "")
                    return false;

                byte[] ba = Encoding.Default.GetBytes(EmvTagList.FirstOrDefault(t => t.Tag == "9f34").Value);
                var hexstring = BitConverter.ToString(ba);
                hexstring = hexstring.Replace("-", "");
                long pinvalue = Convert.ToInt64(hexstring.Trim(), 16);
                var isPinVerifiedBitflags = pinvalue;

                return ((isPinVerifiedBitflags & VERIFIED_PLAIN_ICC) == 1) ||
                       ((isPinVerifiedBitflags & VERIFIED_CYPHER_ONLINE) == 2) ||
                       ((isPinVerifiedBitflags & VERIFIED_PLAIN_ICC_SIG) == 3) ||
                       ((isPinVerifiedBitflags & VERIFIED_CYPHER_ICCG) == 4) ||
                       ((isPinVerifiedBitflags & VERIFIED_CYPHER_ICCG_SIG) == 5);
            }
        }
    }
}

namespace IPA.Core.Data.Entity.Other
{
    public class EmvTag
    {
        public string Tag { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
    }
}
