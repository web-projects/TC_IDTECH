using HidLibrary;
using IDTechSDK;
using IPA.Core.Shared.Enums;
using IPA.Core.Shared.Helpers.StatusCode;
using IPA.DAL.RBADAL.Interfaces;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IPA.DAL.RBADAL.Services
{
    class Device_VP3000 : Device_IDTech
    {
        private IDTechSDK.IDT_DEVICE_Types deviceType;
        private DEVICE_INTERFACE_Types     deviceConnect;
        private DEVICE_PROTOCOL_Types      deviceProtocol;
        private IDTECH_DEVICE_PID          deviceMode;

        private static DeviceInfo deviceInfo;

        public Device_VP3000(IDTECH_DEVICE_PID mode) : base(mode)
        {
            deviceType = IDT_DEVICE_Types.IDT_DEVICE_NONE;
            deviceMode = mode;
            Debug.WriteLine("device: VP3300 instantiated with PID={0}", deviceMode);
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
            if (deviceMode == IDTECH_DEVICE_PID.VP3000_HID)
            {
                string serialNumber = "";
                RETURN_CODE rt = IDT_VP3300.SharedController.config_getSerialNumber(ref serialNumber);

                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    deviceInfo.SerialNumber = serialNumber.Trim('\0');
                    Debug.WriteLine("device INFO[Serial Number]     : {0}", (object) deviceInfo.SerialNumber);
                }
                else
                {
                    Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get serialNumber reason={0}", rt);
                }

                string firmwareVersion = "";
                rt = IDT_VP3300.SharedController.device_getFirmwareVersion(ref firmwareVersion);

                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                    Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);

                    deviceInfo.Port = firmwareVersion.Substring(firmwareVersion.IndexOf("USB", StringComparison.Ordinal), 7);
                    Debug.WriteLine("device INFO[Port]              : {0}", (object) deviceInfo.Port);
                }
                else
                {
                    Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get Firmware version reason={0}", rt);
                }

                deviceInfo.ModelName = IDTechSDK.Profile.IDT_DEVICE_String(deviceType, deviceConnect);
                Debug.WriteLine("device INFO[Model Name]        : {0}", (object) deviceInfo.ModelName);

                deviceInfo.ModelNumber = "UNKNOWN";
                /*rt = IDT_VP3300.SharedController.config_getModelNumber(ref deviceInfo.ModelNumber);

                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    Debug.WriteLine("device INFO[Model Number]      : {0}", (object) deviceInfo.ModelNumber);
                }
                else
                {
                    Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get Model number reason={0}", rt);
                }*/
            }
            else
            {
                // Initialize Device
                HidDevice device = HidDevices.Enumerate(Device_IDTech.IDTechVendorID).FirstOrDefault();

                if (device != null)
                {
                    byte[] data;

                    if(device.ReadSerialNumber(out data))
                    {
                        deviceInfo.SerialNumber = GetKeyboardModeSerialNumber();

                        // VP3300 USB NEO v1.01.107
                        string [] payload = GetKeybaordModeFirmwareVersion();
                        if(payload?.Length == 4)
                        {
                            deviceInfo.FirmwareVersion = payload[3].Substring(1, payload[3].Length -1);
                            deviceInfo.ModelName       = payload[0] + " (" + payload[1] + ")";
                            deviceInfo.ModelNumber     = "UNKNOWN";
                            deviceInfo.Port            = payload[1] + "-KB";
                        }
                    }
                }
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

        private string GetKeyboardModeSerialNumber()
        {
            string serialnumber = "";
            byte[] result;
            try
            {
                byte[] command = { 0x01, 0x56, 0x69, 0x56, 0x4f, 0x74, 0x65, 0x63, 0x68, 0x32, 0x00, 0x12, 0x01, 0x00, 0x00, 0x18, 0xa5 };
                var status = SetupVivoCommand(command, out result);
                if(status == EntryModeStatus.Success)
                {
                    serialnumber = System.Text.Encoding.UTF8.GetString(result, 14, 10);
                }
            }
            catch(Exception exp)
            {
               Debug.WriteLine("DeviceCfg::GetKeyboardModeSerialNumber(): - exception={0}", (object)exp.Message);
            }

            return serialnumber;
        }

        private string [] GetKeybaordModeFirmwareVersion()
        {
            string [] firmwareversion = null;
            byte[] result;
            try
            {
                byte[] command = { 0x01, 0x56, 0x69, 0x56, 0x4f, 0x74, 0x65, 0x63, 0x68, 0x32, 0x00, 0x29, 0x00, 0x00, 0x00, 0xde, 0xa0 };
                var status = SetupVivoCommand(command, out result);
                if(status == EntryModeStatus.Success)
                {
                    //VP3300 USB NEO v1.01.107
                    string worker = System.Text.Encoding.UTF8.GetString(result, 14, result.Length - 16);
                    string [] payload = worker.Split(' ');
                    if(payload.Length == 4)
                    {
                        firmwareversion = payload;
                    }
                }
            }
            catch(Exception exp)
            {
               Debug.WriteLine("DeviceCfg::GetKeybaordModeFirmwareVersion(): - exception={0}", (object)exp.Message);
            }

            return firmwareversion;
        }
        
        private EntryModeStatus SetupVivoCommand(byte[] command, out byte[] response)
        {
            var status = EntryModeStatus.Success;
            const int bufferLength = 1000;
            var deviceDataBuffer = new byte[bufferLength];
            response = null;

            try
            {
                HidDevice device = HidDevices.Enumerate(Device_IDTech.IDTechVendorID).FirstOrDefault();

                if (device != null)
                {
                    // Initialize return data buffer
                    for (int i = 0; i < bufferLength; i++)
                    {
                        deviceDataBuffer[i] = 0;
                    }

                    int featureReportLen = device.Capabilities.FeatureReportByteLength;
              
                    bool result = false;

                    // REPORT: SEND 9 BYTES AT A TIME, WITH THE FIRST BYTE BEING THE REPORT ID
                    int OFFSET = 1;
                    int pages = (command.Length - OFFSET) / 8;
                    int rem   = (command.Length - OFFSET) % 8;

                    for(int i = 0; i <= pages; i++)
                    {
                        // WriteFeatureData works better if we send the entire feature length array, not just the length of command plus checksum
                        var reportBuffer = new byte[featureReportLen];
                        reportBuffer[0] = command[0];
                        if(i < pages)
                        {
                            Array.Copy(command, (8 * i) + OFFSET, reportBuffer, 1, 8);
                        }
                        else
                        {
                            Array.Copy(command, (8 * i) + OFFSET, reportBuffer, 1, rem);
                        }
                        Debug.Write("CMD: [ ");
                        for(int ix = 0; ix < reportBuffer.Length; ix++)
                        {
                            Debug.Write(string.Format("{0:X2} ", reportBuffer[ix]));
                        }
                        Debug.WriteLine(" ] - LEN: {0}", reportBuffer.Length);

                        result = device.WriteFeatureData(reportBuffer);
                    }

                    if (result)
                    {
                        // Empirical data shows this is a good time to wait.
                        Thread.Sleep(1200); 
                        result = ReadFeatureDataLong(out deviceDataBuffer);
                    }

                    //as long as we have data in result, we are ok with failed reading later.
                    if (result || deviceDataBuffer.Length > 0)
                    {
                        int dataIndex = 0;

                        for (dataIndex = bufferLength - 1; dataIndex > 1; dataIndex--)
                        {
                            if (deviceDataBuffer[dataIndex] != 0)
                            {
                                break;
                            }
                        }

                        response = new byte[dataIndex + 1];

                        for (var ind = 0; ind <= dataIndex; ind++)
                        {
                            response[ind] += deviceDataBuffer[ind];
                        }

                        status = EntryModeStatus.Success;
                    }
                    else
                    {
                        status = EntryModeStatus.CardNotRead;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceCfg::SetupVivoCommand(): - exception={0}", (object)ex.Message);
                status = EntryModeStatus.Error;
            }

            return status;
        }

        public override string GetSerialNumber()
        {
           string serialNumber = "";
           RETURN_CODE rt = IDT_VP3300.SharedController.config_getSerialNumber(ref serialNumber);

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
            if(deviceMode == IDTECH_DEVICE_PID.VP3000_HID || deviceMode == IDTECH_DEVICE_PID.VP3000_KYB)
            {
                return deviceInfo;
            }

            return base.GetDeviceInfo();
        }

        #region -- keyboard mode overrides --
        public override void SetVP3000DeviceHidMode()
        {
            byte[] result;
            try
            {
                byte[] command = USDK_VP3000_CONFIGURATION_COMMANDS.SET_DEVICE_HID_MODE;

                Debug.Write("CMD: ");
                for(int i = 0; i < command.Length; i++)
                {
                    Debug.Write(string.Format("{0:X2} ", command[i]));
                }
                Debug.WriteLine("\nLEN: {0}", command.Length);

                var readConfig = new byte[command.Length];
                Array.Copy(command, readConfig, command.Length);

                var status = SetupVivoCommand(readConfig, out result);

                if(status == EntryModeStatus.Success)
                {
                    Debug.Write("RES: [ ");
                    for(int ix = 0; ix < result.Length; ix++)
                    {
                        Debug.Write(string.Format("{0:X2} ", result[ix]));
                    }
                    Debug.WriteLine(" ] - LEN: {0}", result.Length);
                }
            }
            catch(Exception exp)
            {
               Debug.WriteLine("DeviceCfg::SetVP3000DeviceHidMode(): - exception={0}", (object)exp.Message);
            }
        }

        public override void VP3000PingReport()
        {
            byte[] result;
            try
            {
                byte[] command = USDK_VP3000_CONFIGURATION_COMMANDS.PING_REPORT_COMMAND;

                Debug.Write("CMD: ");
                for(int i = 0; i < command.Length; i++)
                {
                    Debug.Write(string.Format("{0:X2} ", command[i]));
                }
                Debug.WriteLine("\nLEN: {0}", command.Length);

                var readConfig = new byte[command.Length];
                Array.Copy(command, readConfig, command.Length);

                var status = SetupVivoCommand(readConfig, out result);
            }
            catch(Exception exp)
            {
               Debug.WriteLine("DeviceCfg::VP3000PingReport(): - exception={0}", (object)exp.Message);
            }
        }
        #endregion
    }

    internal static class USDK_VP3000_CONFIGURATION_COMMANDS
    {
        // POLL COMMAND REPORT:                                               "V     i     V     O     t     e     c     h     2    \0"   CC    SC    LM    LL    DT    CL    CM
        internal static readonly byte [] SET_DEVICE_HID_MODE      = { 0x01, 0x56, 0x69, 0x56, 0x4f, 0x74, 0x65, 0x63, 0x68, 0x32, 0x00, 0x01, 0x01, 0x00, 0x01, 0x01, 0x34, 0xD7 };
        internal static readonly byte [] SET_DEVICE_KEYBOARD_MODE = { 0x01, 0x01, 0x03 };
        internal static readonly byte [] GET_DEVICE_FIRMWARE_VER  = { 0x01, 0x56, 0x69, 0x56, 0x4f, 0x74, 0x65, 0x63, 0x68, 0x32, 0x00, 0x29, 0x00, 0x00, 0x00, 0xde, 0xa0 };
        internal static readonly byte [] PING_REPORT_COMMAND      = { 0x01, 0x56, 0x69, 0x56, 0x4f, 0x74, 0x65, 0x63, 0x68, 0x32, 0x00, 0x18, 0x01, 0x00, 0x00, 0xB3, 0xCD };
    }
}
