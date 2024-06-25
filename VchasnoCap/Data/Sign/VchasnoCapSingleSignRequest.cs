namespace VchasnoCapConsole.VchasnoCap.Data.Sign
{
    sealed class VchasnoCapSingleSignRequest : VchasnoCapSignRequestBase
    {
        public string operationDescription { get; set; }
        public string hash { get; set; }
    }
}
