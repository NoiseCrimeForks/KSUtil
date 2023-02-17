namespace KSUtil
{
    public enum KSUtilCommands
    {
        None = 0,
        Exit = 1,
        Play = 16,
        Step = 20,
        Time = 32,
        Tick = 64,
        Calibration = 128
    }

    public static class UDPCommands
    {
       public static byte[] Exit        = new byte[1]{ (byte)KSUtilCommands.Exit };
       public static byte[] Play        = new byte[1]{ (byte)KSUtilCommands.Play };
       public static byte[] Step        = new byte[1]{ (byte)KSUtilCommands.Step };
       public static byte[] Time        = new byte[1]{ (byte)KSUtilCommands.Time };
       public static byte[] Tick        = new byte[1]{ (byte)KSUtilCommands.Tick };
       // public static byte[] Calibration = new byte[1]{ (byte)KSUtilCommands.Calibration };
    }    
}
