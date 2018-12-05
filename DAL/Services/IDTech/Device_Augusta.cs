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
    class Device_Augusta : Device_IDTech
    {
        private IDTechSDK.IDT_DEVICE_Types deviceType;
        private DEVICE_INTERFACE_Types     deviceConnect;
        private DEVICE_PROTOCOL_Types      deviceProtocol;
        private IDTECH_DEVICE_PID          deviceMode;

        private static DeviceInfo deviceInfo;

        public Device_Augusta(IDTECH_DEVICE_PID mode) : base(mode)
        {
            deviceType = IDT_DEVICE_Types.IDT_DEVICE_NONE;
            deviceMode = mode;
            Debug.WriteLine("device: Augusta instantiated with PID={0}", deviceMode);
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
            RETURN_CODE rt = IDT_Augusta.SharedController.config_getSerialNumber(ref serialNumber);

            if (RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
            {
                deviceInfo.SerialNumber = serialNumber;
                Debug.WriteLine("device INFO[Serial Number]   : {0}", (object) deviceInfo.SerialNumber);
            }
            else
            {
            Debug.WriteLine("DeviceCfg::SetDeviceConfig: failed to get serialNumber e={0}", rt);
            }

            string firmwareVersion = "";
            rt = IDT_Augusta.SharedController.device_getFirmwareVersion(ref firmwareVersion);

            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                Debug.WriteLine("device INFO[Firmware Version]: {0}", (object) deviceInfo.FirmwareVersion);

                deviceInfo.Port = firmwareVersion.Substring(firmwareVersion.IndexOf("USB", StringComparison.Ordinal), 7);
                Debug.WriteLine("device INFO[Port]            : {0}", (object) deviceInfo.Port);
            }

            deviceInfo.ModelName = IDTechSDK.Profile.IDT_DEVICE_String(deviceType, deviceConnect);
            Debug.WriteLine("device INFO[Model Name]      : {0}", (object) deviceInfo.ModelName);

            rt = IDT_Augusta.SharedController.config_getModelNumber(ref deviceInfo.ModelNumber);

            if (RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
            {
                Debug.WriteLine("device INFO[Model Number]    : {0}", (object) deviceInfo.ModelNumber);
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

          if (RETURN_CODE.RETURN_CODE_DO_SUCCESS == rt)
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
            if(deviceMode == IDTECH_DEVICE_PID.AUGUSTA_HID)
            {
                return deviceInfo;
            }

            return base.GetDeviceInfo();
        }
    }
}
