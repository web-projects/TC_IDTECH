using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPA.DAL.RBADAL.Services
{
    class Device_Augusta : Device_IDTech
    {
        public Device_Augusta(IDTECH_DEVICE_PID mode) : base(mode)
        {
        }

        private IDTSetStatus DeviceReset()
        {
            var configStatus = new IDTSetStatus { Success = true };
            // WIP: no resets for these device types
            return configStatus;
        }
    }
}
