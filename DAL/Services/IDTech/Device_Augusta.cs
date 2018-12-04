using IPA.Core.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.DAL.RBADAL.Services
{
    class Device_Augusta : Device_IDTech
    {
        public Device_Augusta() : base((IDTECH_DEVICE_PID.AUGUSTA_HID))
        {

        }
    }
}
