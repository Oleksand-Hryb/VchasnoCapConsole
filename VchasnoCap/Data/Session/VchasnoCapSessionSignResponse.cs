namespace VchasnoCapConsole.VchasnoCap.Data.Session
{
    sealed class VchasnoCapSessionSignResponse : VchasnoCapSessionResponseBase
    {
        public string hash { get; set; }
        public string signature { get; set; }
    }
}
