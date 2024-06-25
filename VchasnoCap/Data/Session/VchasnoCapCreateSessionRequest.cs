namespace VchasnoCapConsole.VchasnoCap.Data.Session
{
    sealed class VchasnoCapCreateSessionRequest
    {
        public string clientId { get; set; }
        public int duration { get; set; } = 86400;
    }
}
