using IDTechSDK;
using IPA.CommonInterface;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigIDTech.Configuration;
using IPA.CommonInterface.ConfigIDTech.Factory;
using IPA.LoggerManager;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Interfaces;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPA.DAL.RBADAL.Services
{
    class Device_Augusta : Device_IDTech
    {
        /********************************************************************************************************/
        // ATTRIBUTES
        /********************************************************************************************************/
        #region -- attributes --
        internal static string _HASH_SHA1_ID_STR = "01";
        internal static string _ENC_RSA_ID_STR   = "01";

        private IDTechSDK.IDT_DEVICE_Types deviceType;
        private DEVICE_INTERFACE_Types     deviceConnect;
        //private DEVICE_PROTOCOL_Types      deviceProtocol;
        private IDTECH_DEVICE_PID          deviceMode;

        private string serialNumber = "";
        private string EMVKernelVer = "";
        private static DeviceInfo deviceInfo = null;
        #endregion

        public Device_Augusta(IDTECH_DEVICE_PID mode) : base(mode)
        {
            deviceType = IDT_DEVICE_Types.IDT_DEVICE_AUGUSTA;
            deviceMode = mode;
            Debug.WriteLine("device: Augusta instantiated with PID={0}", deviceMode);
            Logger.debug( "device: August instantiated with PID={0}", deviceMode);
        }

        public override void Configure(object[] settings)
        {
            deviceType    = (IDT_DEVICE_Types) settings[0];
            deviceConnect = (DEVICE_INTERFACE_Types) settings[1];

            // Create Device info object
            deviceInfo = new DeviceInfo();

            PopulateDeviceInfo();
        }

        private bool PopulateDeviceInfo()
        {
            serialNumber = "";
            RETURN_CODE rt = IDT_Augusta.SharedController.config_getSerialNumber(ref serialNumber);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.SerialNumber = serialNumber;
                Debug.WriteLine("device INFO[Serial Number]     : {0}", (object) deviceInfo.SerialNumber);
            }
            else
            {
                Debug.WriteLine("device: PopulateDeviceInfo() - failed to get serialNumber reason={0}", rt);
            }

            string firmwareVersion = "";
            rt = IDT_Augusta.SharedController.device_getFirmwareVersion(ref firmwareVersion);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                if(deviceMode == IDTECH_DEVICE_PID.AUGUSTA_KYB || deviceMode == IDTECH_DEVICE_PID.AUGUSTAS_KYB)
                {
                    var result = Regex.Match(firmwareVersion, @"^N\v\n");
                    if(result.Success)
                        deviceInfo.FirmwareVersion = firmwareVersion.Substring(result.Index + result.Length);
                    else
                        deviceInfo.FirmwareVersion = firmwareVersion.Trim();
                    deviceInfo.Port = "USB-KB";
                }
                else
                {
                    deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                    deviceInfo.Port = firmwareVersion.Substring(firmwareVersion.IndexOf("USB", StringComparison.Ordinal), 7);
                }
                Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);
                Debug.WriteLine("device INFO[Port]              : {0}", (object) deviceInfo.Port);
            }
            else
            {
                Debug.WriteLine("device: PopulateDeviceInfo() - failed to get Firmware version reason={0}", rt);
            }

            deviceInfo.ModelName = IDTechSDK.Profile.IDT_DEVICE_String(deviceType, deviceConnect);
            Debug.WriteLine("device INFO[Model Name]        : {0}", (object) deviceInfo.ModelName);

            rt = IDT_Augusta.SharedController.config_getModelNumber(ref deviceInfo.ModelNumber);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                if(deviceMode != IDTECH_DEVICE_PID.AUGUSTA_KYB && deviceMode != IDTECH_DEVICE_PID.AUGUSTAS_KYB)
                {
                    deviceInfo.ModelNumber = deviceInfo?.ModelNumber?.Split(' ')[0] ?? "";
                }
                Debug.WriteLine("device INFO[Model Number]      : {0}", (object) deviceInfo.ModelNumber);
            }
            else
            {
                Debug.WriteLine("device: PopulateDeviceInfo() - failed to get Model number reason={0}", rt);
            }

            EMVKernelVer = "";
            rt = IDT_Augusta.SharedController.emv_getEMVKernelVersion(ref EMVKernelVer);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.EMVKernelVersion = EMVKernelVer;
                Debug.WriteLine("device INFO[EMV KERNEL V.]     : {0}", (object) deviceInfo.EMVKernelVersion);
            }
            else
            {
                Debug.WriteLine("device: PopulateDeviceInfo() - failed to get Model number reason={0}", rt);
            }

            return true;
        }

        private IDTSetStatus DeviceReset()
        {
            var configStatus = new IDTSetStatus { Success = true };
            // WIP: no resets for these device types
            return configStatus;
        }

        public override string ParseFirmwareVersion(string firmwareInfo)
        {
            // Augusta format has no space after V: V1.00
            // Validate the format firmwareInfo see if the version # exists
            var version = firmwareInfo.Substring(firmwareInfo.IndexOf('V') + 1,
                                                 firmwareInfo.Length - firmwareInfo.IndexOf('V') - 1).Trim();
            var mReg = Regex.Match(version, @"[0-9]+\.[0-9]+");

            // If the parse succeeded 
            if (mReg.Success)
            {
                version = mReg.Value;
            }

            return version;
        }

        public override string GetSerialNumber()
        {
           string serialNumber = "";
           RETURN_CODE rt = IDT_Augusta.SharedController.config_getSerialNumber(ref serialNumber);

          if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
          {
              deviceInfo.SerialNumber = serialNumber;
              Debug.WriteLine("device::GetSerialNumber(): {0}", (object) deviceInfo.SerialNumber);
          }
          else
          {
            Debug.WriteLine("device::GetSerialNumber(): failed to get serialNumber e={0}", rt);
          }

          return serialNumber;
        }

        public override string GetFirmwareVersion()
        {
            string firmwareVersion = "";
            RETURN_CODE rt = IDT_Augusta.SharedController.device_getFirmwareVersion(ref firmwareVersion);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                firmwareVersion = deviceInfo.FirmwareVersion;
                Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);
            }
            else
            {
                Debug.WriteLine("device: GetDeviceFirmwareVersion() - failed to get Firmware version reason={0}", rt);
            }
            return firmwareVersion;
        }

        public override DeviceInfo GetDeviceInfo()
        {
            if(deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID || deviceMode == IDTECH_DEVICE_PID.AUGUSTAS_HID)
            {
                return deviceInfo;
            }
            else if(deviceMode == IDTECH_DEVICE_PID.AUGUSTA_KYB || deviceMode == IDTECH_DEVICE_PID.AUGUSTAS_KYB)
            {
                PopulateDeviceInfo();
                return deviceInfo;
            }
            return base.GetDeviceInfo();
        }

        /********************************************************************************************************/
        // DEVICE CONFIGURATION
        /********************************************************************************************************/
        #region -- device configuration --

        public void GetTerminalInfo(ConfigSerializer serializer)
        {
            try
            {
                string response = null;
                RETURN_CODE rt = IDT_Augusta.SharedController.device_getFirmwareVersion(ref response);

                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.firmware_ver = response;
                }
                response = "";
                rt = IDT_Augusta.SharedController.emv_getEMVKernelVersion(ref response);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_ver = response;
                }
                response = "";
                rt = IDT_Augusta.SharedController.emv_getEMVKernelCheckValue(ref response);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_checksum = response;
                }
                response = "";
                rt = IDT_Augusta.SharedController.emv_getEMVConfigurationCheckValue(ref response);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_configuration_checksum = response;
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: GetTerminalInfo() - exception={0}", (object)exp.Message);
            }
        }

        public byte [] GetTerminalData(ConfigSerializer serializer, ref int exponent)
        {
            byte [] data = null;

            try
            {
                if(serializer.terminalCfg != null)
                {
                    //int id = IDT_Augusta.SharedController.emv_retrieveTerminalID();

                    RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveTerminalData(ref data);
            
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && data != null)
                    {
                        TerminalData td = new TerminalData(data);
                        string text = td.ConvertTLVToValuePairs();
                        serializer.terminalCfg.general_configuration.Contact.terminal_data = td.ConvertTLVToString();
                        serializer.terminalCfg.general_configuration.Contact.tags = td.GetTags();
                        // Information From Terminal Data
                        string language = td.GetTagValue("DF10");
                        language = (language.Length > 1) ? language.Substring(0, 2) : "";
                        string merchantName = td.GetTagValue("9F4E");
    ///                merchantName = CardReader.ConvertHexStringToAscii(merchantName);
                        string merchantID = td.GetTagValue("9F16");
    ///                merchantID = CardReader.ConvertHexStringToAscii(merchantID);
                        string terminalID = td.GetTagValue("9F1C");
    ///                terminalID = CardReader.ConvertHexStringToAscii(terminalID);
    ///                AUGUSTA SRED FAILS HERE !!! --- CONFIG ISSUE
                       string exp = td.GetTagValue("5F36");
                       if(exp.Length > 0)
                       {
                          exponent = Int32.Parse(exp);
                       }
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: GetTerminalData() - exception={0}", (object)exp.Message);
            }

            return data;
        }

        public void GetCapkList(ref ConfigSerializer serializer)
        {
            try
            {
                if(serializer != null)
                {
                    byte [] keys = null;
                    RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveCAPKList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<Capk> CAPKList = new List<Capk>();

                        foreach(byte[] capk in keys.Split(6))
                        {
                            byte[] key = null;

                            rt = IDT_Augusta.SharedController.emv_retrieveCAPK(capk, ref key);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                Capk apk = new Capk(key);
                                CAPKList.Add(apk);
                            }
                        }

                        // Write to Configuration File
                        if(CAPKList.Count > 0)
                        {
                            serializer.terminalCfg.general_configuration.Contact.capk = CAPKList;
                        }
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: GetCapkList() - exception={0}", (object)exp.Message);
            }
        }
        
        public void GetAidList(ConfigSerializer serializer)
        {
            try
            {
                if(serializer != null)
                {
                    byte [][] keys = null;
                    RETURN_CODE rt = IDT_Augusta.SharedController.emv_retrieveAIDList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<Aid> AidList = new List<Aid>();

                        foreach(byte[] aidName in keys)
                        {
                            byte[] value = null;

                            rt = IDT_Augusta.SharedController.emv_retrieveApplicationData(aidName, ref value);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                Aid aid = new Aid(aidName, value);
                                aid.ConvertTLVToValuePairs();
                                AidList.Add(aid);
                            }
                        }

                        // Write to Configuration File
                        if(AidList.Count > 0)
                        {
                            serializer.terminalCfg.general_configuration.Contact.aid = AidList;
                        }
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: GetAidList() - exception={0}", (object)exp.Message);
            }
        }

        public override void GetMSRSettings(ref ConfigSerializer serializer)
        {
            try
            {
                Msr msr = new Msr();
                List<MSRSettings> msr_settings =  new List<MSRSettings>();; 

                foreach(var setting in msr.msr_settings)
                {
                    byte value   = 0;
                    //RETURN_CODE rt = IDT_Augusta.SharedController.msr_getSetting((byte)setting.function_value, ref value);
                    RETURN_CODE rt = IDT_Augusta.SharedController.msr_getSetting(Convert.ToByte(setting.function_id, 16), ref value);

                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        setting.value = value.ToString("x");
                        msr_settings.Add(setting);
                    }
                }

                serializer.terminalCfg.general_configuration.msr_settings = msr_settings;
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: GetMSRSettings() - exception={0}", (object)exp.Message);
            }
        }

        public void GetEncryptionControl(ConfigSerializer serializer)
        {
            try
            {
                bool msr = false;
                bool icc = false;
                RETURN_CODE rt = IDT_Augusta.SharedController.config_getEncryptionControl(ref msr, ref icc);
            
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    serializer.terminalCfg.general_configuration.Encryption.msr_encryption_enabled = msr;
                    serializer.terminalCfg.general_configuration.Encryption.icc_encryption_enabled = icc;
                    byte format = 0;
                    rt = IDT_Augusta.SharedController.icc_getKeyFormatForICCDUKPT(ref format);
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        string key_format = "None";
                        switch(format)
                        {
                            case 0x00:
                            {
                                key_format = "TDES";
                                break;
                            }
                            case 0x01:
                            {
                                key_format = "AES";
                                break;
                            }
                        }
                        serializer.terminalCfg.general_configuration.Encryption.data_encryption_type = key_format;
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: GetEncryptionControl() - exception={0}", (object)exp.Message);
            }
        }

        public override void CloseDevice()
        {
            if (Profile.deviceIsInitialized(IDT_DEVICE_Types.IDT_DEVICE_AUGUSTA, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB))
            {
                Profile.closeDevice(IDT_DEVICE_Types.IDT_DEVICE_AUGUSTA, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB);
            }
            //if (Profile.deviceIsInitialized(IDT_DEVICE_Types.IDT_DEVICE_AUGUSTA_KB, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB))
            //{
            //    Profile.closeDevice(IDT_DEVICE_Types.IDT_DEVICE_AUGUSTA_KB, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB);
            //}
            IDT_Device.stopUSBMonitoring();
        }

        public override void FactoryReset()
        {
            try
            {
                // TERMINAL DATA
                TerminalDataFactory tf = new TerminalDataFactory();
                byte[] term = tf.GetFactoryTerminalData5C();
                RETURN_CODE rt = IDT_Augusta.SharedController.emv_setTerminalData(term);
                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    Debug.WriteLine("TERMINAL DATA [DEFAULT] ----------------------------------------------------------------------");
                }
                else
                {
                    Debug.WriteLine("TERMINAL DATA [DEFAULT] failed with error code: 0x{0:X}", (ushort) rt);
                }

                // AID
                AidFactory factoryAids = new AidFactory();
                Dictionary<byte [], byte []> aid = factoryAids.GetFactoryAids();
                Debug.WriteLine("AID LIST [DEFAULT] ----------------------------------------------------------------------");
                foreach(var item in aid)
                {
                    byte [] name  = item.Key;
                    byte [] value = item.Value;
                    rt = IDT_Augusta.SharedController.emv_setApplicationData(name, value);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("AID: {0}", (object) BitConverter.ToString(name).Replace("-", string.Empty));
                    }
                    else
                    {
                        Debug.WriteLine("CAPK: {0} failed Error Code: 0x{1:X}", (ushort) rt);
                    }
                }

                // CAPK
                CapKFactory factoryCapk = new CapKFactory();
                Dictionary<byte [], byte []> capk = factoryCapk.GetFactoryCapK();
                Debug.WriteLine("CAPK LIST [DEFAULT] ----------------------------------------------------------------------");
                foreach(var item in capk)
                {
                    byte [] name  = item.Key;
                    byte [] value = item.Value;
                    rt = IDT_Augusta.SharedController.emv_setCAPK(value);

                    if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("CAPK: {0}", (object) BitConverter.ToString(name).Replace("-", string.Empty).ToUpper());
                    }
                    else
                    {
                        Debug.WriteLine("CAPK: {0} failed Error Code: 0x{1:X}", (ushort) rt);
                    }
                }
            }
            catch(Exception exp)
            {
                Debug.WriteLine("device: FactoryReset() - exception={0}", (object)exp.Message);
            }
        }
        #endregion
    }
}
