using System;

namespace KSUtil
{
    public static class Timestamps
    {
        public static string TimeCode { get { return DateTimeOffset.Now.ToString( "yyyyMMdd_HHmmss_ffff" ); } }        
    }
}
