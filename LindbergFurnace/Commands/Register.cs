namespace LindbergFurnace.Commands
{
    public enum Register
    {
      // NOTE: These registers are for the UT 130/150/152/155 models. I BELIEVE we have a UT150
      
      Undefined = -1,
      STATUS =  0x00,
      PV = 0x01, // Measured Input Value
      CSP = 0x02, // Currently Used Target Setpoint
      OUT = 0x03, // Control Output
      HOUT = 0x04, // Heating-side Control Output
      COUT = 0x05, // Cooling-side Control Output
      HC = 0x06, // Heater Current Measured Value
      T1 = 0x07, // Remaining Time Display for A1 setpoint to be reached
      T2 = 0x08,
      SPNO = 0x09, // Target Setpoint Number Selection
      A1 = 0x64, //Alarm or Timer Setpoint
      A2 = 0x65,
      CTL = 0x66,
      AT = 0x67,
      P = 0x68,
      I = 0x69,
      D = 0x6A,
      MR = 0x6B, // Manual Reset
      COL = 0x6C,
      DB = 0x6D,
      HYS = 0x6E,
      CT = 0x6F,
      CTC = 0x70,
      SP1 = 0x71,
      SP2 = 0x72,
      FL = 0x73,
      BS = 0x74,
      LOC = 0x75,
      CSP1 = 0x77, // Target Setpoint for Writing via Communication Only. Only effective if SP1 register is selected. Same value written in this D register is also written in SP1
      UPR = 0xC8,
      DNR = 0xC9,
      AL1 = 0xCA,
      AL2 = 0xCB,
      HY1 = 0xCC,
      HY2 = 0xCD,
      SC = 0xCE,
      DR = 0xCF,
      DSP = 0xD0,
      PSL = 0xD1,
      ADR = 0xD2,
      BPS = 0xD3,
      PRI = 0xD4,
      STP = 0xD5,
      DLN = 0xD6,
      IN = 0x12C,
      DP = 0x12B,
      RH = 0x12E,
      RL = 0x12F,
      SPH = 0x130,
      SPL = 0x131,
      TMU = 0x132,
      DIS = 0x133,
      EOT = 0x134,
      TTU = 0x135,
      RTL = 0x136,
      RTH = 0x137,
    }
}
