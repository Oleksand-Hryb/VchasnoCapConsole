namespace VchasnoCapConsole.VchasnoCap.Data.Session
{
    sealed class VchasnoCapCreateSessionResponse : VchasnoCapSessionResponseBase
    {
        public string authSessionId { get; set; }
    }
}
