namespace VchasnoCapConsole.VchasnoCap.Data.Session
{
    sealed class VchasnoCapSessionSignRequest
    {
        public string authSessionToken { get; set; }
        public string clientId { get; set; }
        public string password { get; set; }
        public string hash { get; set; }
    }
}
