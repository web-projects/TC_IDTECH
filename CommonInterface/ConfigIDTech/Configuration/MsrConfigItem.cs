using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigIDTech.Configuration
{
  [Serializable]
  public class MsrConfigItem
  {  
    public string Name { get; set; }  
    public string Value { get; set; }

    public int Id { get; set; }
  }

  // EXPIRATION MASK
  public enum EXPIRATION_MASK
  {
    MASK = 0x00
  }

  // PAN DIGITS
  public enum PAN_DIGITS
  {
    DIGITS = 0x00,
  }

  // SWIPE FORCE
  public enum SWIPE_FORCE_ENCRYPTION
  {
    TRACK1 = 0x01,
    TRACK2 = 0X02,
    TRACK3 = 0X03,
    TRACK3CARD0 = 0X04,
  }

  // SWIPE MASK
  public enum SWIPE_MASK
  {
    TRACK1 = 0x01,
    TRACK2 = 0X02,
    TRACK3 = 0X03,
  }
}
