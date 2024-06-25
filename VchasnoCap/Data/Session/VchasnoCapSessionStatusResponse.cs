namespace VchasnoCapConsole.VchasnoCap.Data.Session
{
    sealed class VchasnoCapSessionStatusResponse : VchasnoCapSessionResponseBase
    {
        public string status { get; set; }
        public string token { get; set; }
    }
}
