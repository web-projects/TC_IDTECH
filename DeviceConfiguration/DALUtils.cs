using IPA.DAL.RBADAL.Services.Devices.IDTech;
using IPA.DAL.RBADAL.Services.Devices.IDTech.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IPA.DAL.RBADAL
{
    public enum InitialCrcValue { ZEROS = 0x0000, NONZERO1 = 0xffff, NONZERO2 = 0x1D0F }

    public class DALUtils
    {
        // constants
        const int StxValid = 2;
        const int EtxValid = 3;

        const int osStx = 0;
        const int osLenDataL = 1;
        const int osLenDataH = 2;
        const int osCardEncodeType = 3;
        const int osTrackStatus = 4;
        const int osLenT1 = 5;
        const int osLenT2 = 6;     //LenPanData
        const int osLenT3 = 7;     //LenAddrZip
        const int osMaskStatus = 8;
        const int osDataStatus = 9;

        const int lenHash = 20;
        const int lenKsn = 10;
        const int lenSerialNumber = 10;

        public static TrackData GetTrackData(byte[] data)
        {
            var isXmlFormat = Encoding.ASCII.GetString(SubArray<byte>(data, 0, 7)) == "<DvcMsg";
            TrackData trackData = isXmlFormat ? ParseXmlFormat(data) : ParseIdtFormat(data);
            return trackData;
        }
        public static TrackData ParseIdtFormat(byte[] bytes)
        {
            var test = ByteArrayToHexString(bytes);

            var testdata = HexStringToByteArray(test);
            //get length data
            var lenData = BitConverter.ToInt16(new byte[] { bytes[osLenDataL], bytes[osLenDataH] }, 0);
            var hexlenData = bytes.Length - 6;
            //validate data

            var data = SubArray<byte>(bytes, 3, lenData);
            if (bytes[lenData + 3] != GetLrc(data) ||
                bytes[lenData + 4] != GetChecksumAdd(data) ||
                bytes[osStx] != StxValid ||
                bytes[lenData + 5] != EtxValid)
                throw new Exception("invalid card read");

            // get the actual idtech data
            string idtData = test.Substring(0, (lenData + 6) * 2);

            //// get the actual idtech data
            //string idtData = test.Substring(0, (lenData +6)*2);
            //get card encode type and manual
            var cardEncodeType = (CardEncodeType)bytes[osCardEncodeType];
            var isEnhancedFormat = cardEncodeType == CardEncodeType.AAMVA_Enhanced || cardEncodeType == CardEncodeType.ISOABA_Enhanced || cardEncodeType == CardEncodeType.manual_Enhanced || cardEncodeType == CardEncodeType.Other_Enhanced || cardEncodeType == CardEncodeType.Raw_Enhanced;  //isEnhancedFormat==true
            var isManual = cardEncodeType == CardEncodeType.manual || cardEncodeType == CardEncodeType.manual_Enhanced;

            TrackData trackData;
            if (cardEncodeType == CardEncodeType.manual)
            {
                var trackStatus = bytes[8];//should be 04
                var lenUnEncryptData = bytes[9];//length of unencrypted data (PAN=EXP= from the status bits)  - 16 (22 dec)                                               
                int lenEncryptData = lenUnEncryptData % 8 > 0 ? (lenUnEncryptData / 8 + 1) * 8 : lenUnEncryptData; //Note: encrypted data length is len of unencrypted data rounded up to a multiple of 8 bytes(64 - bits) – I think (please test with other lengths of PAN data)
                var encryptedData = ByteArrayToHexString(SubArray<byte>(bytes, 10, lenEncryptData));
                var hashData = ByteArrayToHexString(SubArray<byte>(bytes, 10 + lenEncryptData, 20));
                var expLenStr = ByteArrayToHexString(SubArray<byte>(bytes, 10 + lenEncryptData + 20, 1));
                int expLen = Int32.Parse(expLenStr.TrimStart('0'));
                var exp = Encoding.ASCII.GetString(SubArray<byte>(bytes, 10 + lenEncryptData + 20 + 1, expLen));
                var ksn = ByteArrayToHexString(SubArray<byte>(bytes, 10 + lenEncryptData + 20 + 1 + expLen, 10));
                string track2Data = new string('*', lenUnEncryptData - expLen - 3);
                track2Data = $";{track2Data}={exp}=";

                trackData = new TrackData()
                {
                    T1Data = "",
                    T2Data = "",
                    T3Data = "",

                    T1Crypto = "",
                    T2Crypto = "",
                    T3Crypto = "",

                    T1Hash = "",
                    T2Hash = "",
                    T3Hash = "",

                    SerialNumber = "",
                    Ksn = ksn,

                    //EncryptedTracks = ConvertIDTByteArrayToString(bytes),
                    EncryptedTracks = idtData,
                    IsSwipe = !isManual
                };

                trackData.PAN = "";
                trackData.Name = "";
                trackData.ExpDate = exp;
                trackData.T3Data = encryptedData;

            }
            else if (isEnhancedFormat)
            {
                var trackStatus = (TrackStatus)bytes[osTrackStatus];
                var maskStatus = (MaskStatus)bytes[osMaskStatus];
                var dataStatus = (CryptoStatus)bytes[osDataStatus];
                var isAesCrypto = maskStatus.HasFlag(MaskStatus.AesEncryption);
                var cryptoMultiple = (isAesCrypto ? 16 : 8);

                var serialNumberLen = maskStatus.HasFlag(MaskStatus.SerialNumberPresent) ? lenSerialNumber : 0;
                var ksnLen = dataStatus.HasFlag(CryptoStatus.KsnPresent) ? lenKsn : 0;

                var lenT1 = bytes[osLenT1];
                var lenT2 = bytes[osLenT2];
                var lenT3 = bytes[osLenT3];
                var t1DataLen = trackStatus.HasFlag(TrackStatus.T1Sampling) ? lenT1 : 0;
                var t2DataLen = trackStatus.HasFlag(TrackStatus.T2Sampling) ? lenT2 : 0;
                var t3DataLen = trackStatus.HasFlag(TrackStatus.T3Sampling) ? lenT3 : 0;

                var lenT1CryptoPad = ((lenT1 % cryptoMultiple) > 0 ? cryptoMultiple : 0) - (lenT1 % cryptoMultiple);
                var lenT2CryptoPad = ((lenT2 % cryptoMultiple) > 0 ? cryptoMultiple : 0) - (lenT2 % cryptoMultiple);
                var lenT3CryptoPad = ((lenT3 % cryptoMultiple) > 0 ? cryptoMultiple : 0) - -(lenT3 % cryptoMultiple);
                var t1CryptoLen = dataStatus.HasFlag(CryptoStatus.T1Encrypted) ? lenT1 + lenT1CryptoPad : 0;
                var t2CryptoLen = dataStatus.HasFlag(CryptoStatus.T2Encrypted) ? lenT2 + lenT2CryptoPad : 0;
                var t3CryptoLen = dataStatus.HasFlag(CryptoStatus.T3Encrypted) ? lenT3 + lenT3CryptoPad : 0;
                var t1HashLen = dataStatus.HasFlag(CryptoStatus.T1Hash) ? lenHash : 0;
                var t2HashLen = dataStatus.HasFlag(CryptoStatus.T2Hash) ? lenHash : 0;
                var t3HashLen = dataStatus.HasFlag(CryptoStatus.T3Hash) ? lenHash : 0;


                var osT1Data = 10;
                var osT2Data = osT1Data + t1DataLen;
                var osT3Data = osT2Data + t2DataLen;
                var osT1Crypto = osT3Data + t3DataLen;
                var osT2Crypto = osT1Crypto + t1CryptoLen;
                var osT3Crypto = osT2Crypto + t2CryptoLen;
                var osT1Hash = osT3Crypto + t3CryptoLen;
                var osT2Hash = osT1Hash + t1HashLen;
                var osT3Hash = osT2Hash + t2HashLen;

                var osSerialNumber = osT3Hash + t3HashLen;
                var osKsn = osSerialNumber + serialNumberLen;
                var osLrc = osKsn + ksnLen;
                var osCheckSum = osLrc + 1;
                var osEtx = osCheckSum + 1;

                trackData = new TrackData()
                {
                    T1Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, osT1Data, t1DataLen)),
                    T2Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, osT2Data, t2DataLen)),
                    T3Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, osT3Data, t3DataLen)),

                    T1Crypto = ByteArrayToHexString(SubArray<byte>(bytes, osT1Crypto, t1CryptoLen)),
                    T2Crypto = ByteArrayToHexString(SubArray<byte>(bytes, osT2Crypto, t2CryptoLen)),
                    T3Crypto = ByteArrayToHexString(SubArray<byte>(bytes, osT3Crypto, t3CryptoLen)),

                    T1Hash = ByteArrayToHexString(SubArray<byte>(bytes, osT1Hash, t1HashLen)),
                    T2Hash = ByteArrayToHexString(SubArray<byte>(bytes, osT2Hash, t2HashLen)),
                    T3Hash = ByteArrayToHexString(SubArray<byte>(bytes, osT3Hash, t3HashLen)),

                    SerialNumber = ByteArrayToHexString(SubArray<byte>(bytes, osSerialNumber, serialNumberLen)),
                    Ksn = ByteArrayToHexString(SubArray<byte>(bytes, osKsn, ksnLen)),

                    EncryptedTracks = ConvertIDTByteArrayToString(bytes),
                    IsSwipe = !isManual
                };

                var track1Values = GetTrack1(trackData.T1Data);
                trackData.PAN = track1Values.PAN;
                trackData.Name = track1Values.Name ?? track1Values.Name;
                trackData.ExpDate = track1Values.ExpDate;

                //debit card has not track1 data
                if (trackData.T1Data == string.Empty)
                {
                    string track1 = trackData.T2Data;
                    track1 = track1.Replace(";", "%%");
                    track1 = track1.Replace('*', '0');
                    track1 = track1.Replace('?', '0');
                    track1 = track1.Replace("=", "^MANUALLY/ENTERED^");
                    track1Values = GetTrack1(track1);
                    trackData.PAN = track1Values.PAN;
                    trackData.Name = "";
                    trackData.ExpDate = track1Values.ExpDate;
                }

                //Matthew: use ingenigo format for IDT data 
                if (String.IsNullOrWhiteSpace(trackData.T2Data) && String.IsNullOrWhiteSpace(trackData.T3Data))
                {
                    trackData.T3Data = $"{trackData.Ksn}:1:{trackData.T1Crypto.Length / 2:D4}:{trackData.T1Crypto}";
                }
                else if (String.IsNullOrWhiteSpace(trackData.T3Data) && !String.IsNullOrWhiteSpace(trackData.T2Crypto))
                {
                    trackData.T3Data = $"{trackData.Ksn}:2:{trackData.T2Crypto.Length / 2:D4}:{trackData.T2Crypto}";
                    trackData.T1Data = string.Empty;
                }
                if (isManual)
                {
                    string firstHalf = ByteArrayToHexString(SubArray<byte>(bytes, 3, osT2Data - 3)) + Encoding.ASCII.GetString(SubArray<byte>(bytes, osT2Data, t2DataLen));
                    string newData = firstHalf + idtData.Substring(firstHalf.Length + t2DataLen + 6);
                    int dataLength = newData.Length - 6;
                    string dataLengthHex = dataLength.ToString("X");

                    newData = newData.Substring(0, dataLength);

                    //var newDataHex =HexStringToByteArray(newData);
                    long LRC = 0;
                    long ChkSum = 0;
                    for (int i = 0; i < newData.Length; i++)
                    {
                        long l = Convert.ToInt64(newData[i]);
                        LRC ^= l;
                        ChkSum += l;
                    }
                    byte[] LRCArray = BitConverter.GetBytes(LRC);
                    byte[] ChkSumArray = BitConverter.GetBytes(ChkSum);
                    byte[] ending = new byte[3];
                    ending[0] = LRCArray[0];
                    ending[1] = ChkSumArray[0];
                    ending[2] = EtxValid;
                    newData = "02" + dataLengthHex + "00" + newData + ByteArrayToHexString(ending);
                    trackData.EncryptedTracks = newData;
                }
                else
                {
                    trackData.EncryptedTracks = $"{trackData.T1Data}|{trackData.T2Data}|{trackData.T3Data}";
                }


            }
            else if (cardEncodeType == CardEncodeType.ISOABA)//original format
            {
                var trackStatus = bytes[4];//should be 04
                var lenUnEncryptTrack1 = bytes[5];//length of unencrypted data (PAN=EXP= from the status bits)  - 16 (22 dec)    
                var lenUnEncryptTrack2 = bytes[6];
                var lenUnEncryptTrack3 = bytes[7];
                int indexUnEcryptTrack1 = 8;
                int indexUnEcryptTrack2 = indexUnEcryptTrack1 + lenUnEncryptTrack1;
                int indexUnEcryptTrack3 = indexUnEcryptTrack2 + lenUnEncryptTrack2;

                int lenEncryptTrack = (lenUnEncryptTrack1 + lenUnEncryptTrack2 + lenUnEncryptTrack3) % 8 > 0 ?
                    ((lenUnEncryptTrack1 + lenUnEncryptTrack2 + lenUnEncryptTrack3) / 8 + 1) * 8 : (lenUnEncryptTrack1 + lenUnEncryptTrack2 + lenUnEncryptTrack3);
                int indexEncryptTrack = indexUnEcryptTrack3 + lenUnEncryptTrack3;

                int lenHash = 20;
                int lenHashTrack1 = 0;
                int lenHashTrack2 = 0;
                int indexHashTrack1 = indexEncryptTrack + lenEncryptTrack;

                if (lenUnEncryptTrack1 > 0)//we have track1, we have track1 hash.
                {
                    lenHashTrack1 = lenHash;
                }
                int indexHashTrack2 = indexHashTrack1 + lenHashTrack1;

                if (lenUnEncryptTrack2 > 0)//we have track2, we have track2 hash.
                {
                    lenHashTrack2 = lenHash;
                }
                int indexKsn = indexHashTrack2 + lenHashTrack2;
                int lenKsn = 10;

                trackData = new TrackData()
                {
                    T1Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, indexUnEcryptTrack1, lenUnEncryptTrack1)),
                    T2Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, indexUnEcryptTrack2, lenUnEncryptTrack2)),
                    T3Data = Encoding.ASCII.GetString(SubArray<byte>(bytes, indexUnEcryptTrack3, lenUnEncryptTrack3)),

                    T1Crypto = ByteArrayToHexString(SubArray<byte>(bytes, indexEncryptTrack, lenEncryptTrack)),
                    T2Crypto = "",
                    T3Crypto = "",

                    T1Hash = ByteArrayToHexString(SubArray<byte>(bytes, indexHashTrack1, lenHashTrack1)),
                    T2Hash = ByteArrayToHexString(SubArray<byte>(bytes, indexHashTrack2, lenHashTrack2)),
                    T3Hash = "",

                    SerialNumber = "",
                    Ksn = ByteArrayToHexString(SubArray<byte>(bytes, indexKsn, lenKsn)),

                    EncryptedTracks = "",
                    IsSwipe = !isManual
                };

                var track1Values = GetTrack1(trackData.T1Data);
                trackData.PAN = track1Values.PAN;
                trackData.Name = track1Values.Name ?? track1Values.Name;
                trackData.ExpDate = track1Values.ExpDate;

                //debit card has not track1 data
                if (trackData.T1Data == string.Empty)
                {
                    string track1 = trackData.T2Data;
                    track1 = track1.Replace(";", "%%");
                    track1 = track1.Replace('*', '0');
                    track1 = track1.Replace('?', '0');
                    track1 = track1.Replace("=", "^MANUALLY/ENTERED^");
                    track1Values = GetTrack1(track1);
                    trackData.PAN = track1Values.PAN;
                    trackData.Name = "";
                    trackData.ExpDate = track1Values.ExpDate;
                }
                if (lenUnEncryptTrack2 > 0)//if we do not have track2, it means it is an invalid swipe or card.
                    trackData.T3Data = $"{trackData.Ksn}:4:{(trackData.T1Crypto.Length) / 2:D4}:{trackData.T1Crypto}";
                else
                    trackData.T3Data = string.Empty;

                trackData.EncryptedTracks = $"{trackData.T1Data}|{trackData.T2Data}|{trackData.T3Data}";
            }
            else
            {
                trackData = null;
            }
            if (String.IsNullOrWhiteSpace(trackData?.Ksn) || String.IsNullOrWhiteSpace(trackData?.T3Data))
                trackData = null;

            return trackData;
        }

        private static Track1 GetTrack1(string t1Data)
        {
            try
            {
                const int lenExpDate = 4;

                var startSentinel = t1Data.IndexOf('%');
                if (startSentinel != 0)
                    throw new Exception("Track1|StartSentinel Missing");

                var panSeparator = t1Data.IndexOf('^', startSentinel + 1);
                if (panSeparator == 0)
                    throw new Exception("Track1|PanSeparator Missing");

                var nameSeparator = t1Data.IndexOf('^', panSeparator + 1);
                if (nameSeparator == 0)
                    throw new Exception("Track1|NameSeparator Missing");

                var endSentinel = t1Data.IndexOf('?', nameSeparator + 1);
                if (endSentinel == 0)
                    throw new Exception("Track1|EndSeparator Missing");

                var lenPan = panSeparator - startSentinel - 2;
                if (lenPan > 19)   //TODO: regex validation
                    throw new Exception("Track1|Invalid PAN");

                var lenName = nameSeparator - panSeparator - 1;
                if (lenName > 26 || lenName < 2)  //TODO: regex validation
                    throw new Exception("Track1|Invalid Name");

                var ExpData = t1Data.Substring(nameSeparator + 1, 4);
                if (ExpData.Length != 4)  //TODO: regex validation
                    throw new Exception("Track1|Invalid ExpDate");

                return new Track1()
                {
                    PAN = t1Data.Substring(startSentinel + 2, lenPan),
                    Name = t1Data.Substring(panSeparator + 1, lenName),
                    ExpDate = t1Data.Substring(nameSeparator + 1, lenExpDate)
                };
            }
            catch (Exception ex)
            {
                var t = ex;
                //LOG error
                return new Track1();
            }
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public static string ConvertIDTByteArrayToString(byte[] values)
        {
            var hex1 = ByteArrayToHexString(SubArray<byte>(values, osStx, osDataStatus + 1));
            var asc1 = Encoding.ASCII.GetString(SubArray<byte>(values, osDataStatus + 1, values[osLenT1] + values[osLenT2] + values[osLenT3]));
            var hex2 = ByteArrayToHexString(SubArray<byte>(values, hex1.Length / 2 + asc1.Length, values.Length - hex1.Length / 2 - asc1.Length));

            var data = new StringBuilder(hex1.Length + asc1.Length + hex2.Length);
            data.Append(hex1).Append(asc1).Append(hex2);

            data.Remove(2, 4).Insert(2, ByteArrayToHexString(BitConverter.GetBytes(Convert.ToInt16(data.Capacity - 12))));
            //note: ITC checksum/lrc unmodified

            var d = data.ToString();       //IDT HEX_ASCII_HEX mixedmode string
            var t = GetLrc(ASCIIEncoding.ASCII.GetBytes(d.Substring(6, d.Length - 12)));
            var r = GetChecksumAdd(ASCIIEncoding.ASCII.GetBytes(d.Substring(6, d.Length - 12)));

            return d;
        }

        static bool match(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        static int search(byte[] haystack, byte[] needle, int start)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                if (match(haystack, needle, i))
                {
                    return i;
                }
            }
            return -1;
        }
        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            try
            {
                Array.Copy(data, index, result, 0, length);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("utils: exception in SubArray - ex={0}", (object) ex.Message);
            }
            return result;
        }

        public static string ByteArrayToHexString(byte[] values)
        {
            return BitConverter.ToString(values).Replace("-", "");
        }

        public static TrackData ParseXmlFormat(byte[] bytes)
        {
            var test = Encoding.ASCII.GetString(bytes);
            var testdata = Encoding.ASCII.GetBytes(test);

            var osDvcMsgEnd = search(bytes, Encoding.ASCII.GetBytes("</DvcMsg>"), 0);
            var osCard = search(bytes, Encoding.ASCII.GetBytes("<Card "), 0);
            var osCardEnd = search(bytes, Encoding.ASCII.GetBytes("></Card"), osCard);
            bool osIsSwipe = true;
            int osETrk1 = 0;
            int osETrk1End = 0;
            int osETrk2 = 0;
            int osETrk2End = 0;
            int osECData = 0;
            int osECDataEnd = 0;
            int cardEntry = search(bytes, Encoding.ASCII.GetBytes("Entry=\"MANUAL\""), 0);
            if (cardEntry > 0)//manual
            {
                osIsSwipe = false;
                osECData = search(bytes, Encoding.ASCII.GetBytes("ECData=\""), osCard);
                if (osECData < 0)
                    osECData = 0;
                osECDataEnd = search(bytes, Encoding.ASCII.GetBytes("\" CDataKSN=\""), osECData);
            }
            else
            {
                try
                {
                    osETrk1 = search(bytes, Encoding.ASCII.GetBytes("ETrk1=\""), osCard);
                    if (osETrk1 < 0)
                        osETrk1 = 0;
                    osETrk1End = search(bytes, Encoding.ASCII.GetBytes("\" ETrk2=\""), osETrk1);
                }
                catch (Exception)
                {

                }
                osETrk2 = search(bytes, Encoding.ASCII.GetBytes("ETrk2=\""), osETrk1End);
                osETrk2End = search(bytes, Encoding.ASCII.GetBytes("\" CDataKSN=\""), osETrk2);

            }


            var osKsn = search(bytes, Encoding.ASCII.GetBytes("CDataKSN=\""), osETrk2);
            var osKsnEnd = search(bytes, Encoding.ASCII.GetBytes("\" Exp=\""), osKsn);

            const int MaxResponseLength = 666;
            var data = new StringBuilder(MaxResponseLength);
            if (osETrk1 > 0)
            {
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, 0, osETrk1 + 7)));
                data.Append(ByteArrayToHexString(((SubArray<byte>(bytes, osETrk1 + 7, osETrk1End - osETrk1 - 7)))));
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osETrk1End, osETrk2 - osETrk1End + 7)));
            }
            if (osETrk2 > 0)
            {
                data.Append(ByteArrayToHexString(SubArray<byte>(bytes, osETrk2 + 7, osETrk2End - osETrk2 - 7)));
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osETrk2End, osKsn - osETrk2End + 10)));
            }
            if (osECData > 0) //manual input
            {
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, 0, osECData + 8)));
                data.Append(ByteArrayToHexString(((SubArray<byte>(bytes, osECData + 8, osECDataEnd - osECData - 8)))));
                data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osECDataEnd, osKsn - osECDataEnd + 10)));
            }

            data.Append(ByteArrayToHexString(SubArray<byte>(bytes, osKsn + 10, osKsnEnd - osKsn - 10)));
            data.Append(Encoding.ASCII.GetString(SubArray<byte>(bytes, osKsnEnd, osDvcMsgEnd - osKsnEnd + 9)));
            var dvcMsg = data.ToString();
            dvcMsg = dvcMsg.Replace("\0", " ");//remove the "\0" null value.
            XmlSerializer serializer = new XmlSerializer(typeof(DvcMsg));
            StringReader rdr = new StringReader(dvcMsg);
            var obj = (DvcMsg)serializer.Deserialize(rdr);

            var trackData = new TrackData()
            {
                IsDebit = false,
                T1Data = obj.Card.ETrk1,
                T2Data = obj.Card.ETrk2,
                Name = obj.Card.CHolder ?? string.Empty,
                ExpDate = obj.Card.Exp.ToString(),
                PAN = obj.Card.MskPAN,
                EncryptedTracks = "",
                T1Crypto = obj.Card.ETrk1,
                T2Crypto = obj.Card.ETrk2,
                Ksn = obj.Card.CDataKSN,
                IsSwipe = osIsSwipe
            };

            //trackData.SerialNumber = obj.Dvc.DvcSN.ToString();

            //trackData.IsSwipe = true;

            //Matthew: use ingenigo format for IDT data 
            if (String.IsNullOrWhiteSpace(trackData.T1Crypto) && String.IsNullOrWhiteSpace(trackData.T2Crypto))
            {
                trackData.IsSwipe = false;
                trackData.PAN = trackData.PAN.Replace('*', '0');
                trackData.T1Data = $"";
                trackData.T2Data = $"";
                trackData.T3Data = $"";
            }
            else if (String.IsNullOrWhiteSpace(trackData.T2Data))
            {
                trackData.T3Data = $"{trackData.Ksn}:1:{trackData.T1Crypto.Length / 2:D4}:{trackData.T1Crypto}";
            }
            else
            {
                trackData.T3Data = $"{trackData.Ksn}:2:{trackData.T2Crypto.Length / 2:D4}:{trackData.T2Crypto}";
                trackData.T1Data = string.Empty;
            }
            if (trackData.IsSwipe)
                trackData.EncryptedTracks = $"{trackData.T1Data}|{trackData.T2Data}|{trackData.T3Data}";
            else
            {
                trackData.EncryptedTracks = dvcMsg;
            }

            return trackData;
        }

        public static byte GetChecksumAdd(byte[] data)
        {
            long longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        public static byte GetLrc(byte[] bytes)
        {
            byte LRC = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                LRC ^= bytes[i];
            }
            return LRC;
        }
    }

    public class Crc16Ccitt 
    {
        const ushort poly = 4129;
        ushort[] table = new ushort[256];
        ushort initialValue = 0;

        public Crc16Ccitt(InitialCrcValue initialValue)
        {
            this.initialValue = (ushort)initialValue;
            ushort temp, a;
            for(int i = 0; i < table.Length; ++i) 
            {
                temp = 0;
                a = (ushort)(i << 8);
                for(int j = 0; j < 8; ++j) 
                {
                    if(((temp ^ a) & 0x8000) != 0) 
                    {
                        temp = (ushort)((temp << 1) ^ poly);
                    } 
                    else 
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                table[i] = temp;
            }
        }

        public ushort ComputeChecksum(byte[] bytes, int length)
        {
            ushort crc = this.initialValue;
            int MAX_LEN = Math.Min(bytes.Length, length);
            for(int i = 0; i < MAX_LEN; ++i) 
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes, int length) 
        {
            ushort crc = ComputeChecksum(bytes, length);
            return BitConverter.GetBytes(crc);
        }
    }
}
