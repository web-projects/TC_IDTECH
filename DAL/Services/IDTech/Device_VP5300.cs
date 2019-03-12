using IDTechSDK;
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
    class Device_VP5300 : Device_IDTech
    {
        /********************************************************************************************************/
        // ATTRIBUTES
        /********************************************************************************************************/
        #region -- attributes --
        private IDTechSDK.IDT_DEVICE_Types deviceType;
        private DEVICE_INTERFACE_Types     deviceConnect;
        private DEVICE_PROTOCOL_Types      deviceProtocol;
        private IDTECH_DEVICE_PID          deviceMode;

        private static DeviceInfo deviceInfo;
        #endregion

        public Device_VP5300(IDTECH_DEVICE_PID mode) : base(mode)
        {
            deviceType = IDT_DEVICE_Types.IDT_DEVICE_NEO2;
            deviceMode = mode;
            Debug.WriteLine("device: VP5300 instantiated with PID={0}", deviceMode);
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
            string serialNumber = "";
            RETURN_CODE rt = IDT_NEO2.SharedController.config_getSerialNumber(ref serialNumber);

            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.SerialNumber = serialNumber;
                Debug.WriteLine("device INFO[Serial Number]     : {0}", (object) deviceInfo.SerialNumber);
            }
            else
            {
                Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get serialNumber reason={0}", rt);
            }

            string firmwareVersion = "";
            rt = IDT_NEO2.SharedController.device_getFirmwareVersion(ref firmwareVersion);

            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);

                //deviceInfo.Port = firmwareVersion.Substring(firmwareVersion.IndexOf("USB", StringComparison.Ordinal), 7);
                deviceInfo.Port = "USB-HID";
                Debug.WriteLine("device INFO[Port]              : {0}", (object) deviceInfo.Port);
            }
            else
            {
                Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get Firmware version reason={0}", rt);
            }

            deviceInfo.ModelName = IDTechSDK.Profile.IDT_DEVICE_String(deviceType, deviceConnect);
            Debug.WriteLine("device INFO[Model Name]        : {0}", (object) deviceInfo.ModelName);

            rt = IDT_SpectrumPro.SharedController.config_getModelNumber(ref deviceInfo.ModelNumber);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.ModelNumber = deviceInfo?.ModelNumber?.Split(' ')[0] ?? "";
                Debug.WriteLine("device INFO[Model Number]      : {0}", (object) deviceInfo.ModelNumber);
            }
            else
            {
                Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get Model number reason={0}", rt);
            }

            return true;
        }

        private IDTSetStatus DeviceReset()
        {
            var configStatus = new IDTSetStatus { Success = true };
            // WIP: no resets for these device types
            return configStatus;
        }

        public override string GetFirmwareVersion()
        {
            string firmwareVersion = "";
            RETURN_CODE rt = IDT_NEO2.SharedController.device_getFirmwareVersion(ref firmwareVersion);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                //deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                //firmwareVersion = deviceInfo.FirmwareVersion;
                deviceInfo.FirmwareVersion = firmwareVersion;
                Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);
            }
            else
            {
                Debug.WriteLine("device: GetDeviceFirmwareVersion() - failed to get Firmware version reason={0}", rt);
            }
            return firmwareVersion;
        }

        public override string ParseFirmwareVersion(string firmwareInfo)
        {
            // Augusta format has no space after V: V1.00
            // Validate the format firmwareInfo see if the version # exists
            var version = firmwareInfo.Substring(firmwareInfo.IndexOf('V') + 1,
                                                 firmwareInfo.Length - firmwareInfo.IndexOf('V') - 1).Trim();
            var mReg = Regex.Match(version, @"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+");

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
           RETURN_CODE rt = IDT_VP8800.SharedController.config_getSerialNumber(ref serialNumber);

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

        public override DeviceInfo GetDeviceInfo()
        {
            if(deviceMode == IDTECH_DEVICE_PID.VP5300_HID)
            {
                return deviceInfo;
            }

            return base.GetDeviceInfo();
        }

        public override void CloseDevice()
         {
            if (Profile.deviceIsInitialized(IDT_DEVICE_Types.IDT_DEVICE_NEO2, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB))
            {
                Profile.closeDevice(IDT_DEVICE_Types.IDT_DEVICE_NEO2, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB);
            }
            IDT_Device.stopUSBMonitoring();
         }
    }
}
