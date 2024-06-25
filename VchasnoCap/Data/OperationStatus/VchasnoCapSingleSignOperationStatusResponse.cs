using VchasnoCapConsole.VchasnoCap.Data.Common;

namespace VchasnoCapConsole.VchasnoCap.Data.OperationStatus
{
    sealed class VchasnoCapSingleSignOperationStatusResponse : VchasnoCapResponseBase
    {
        public string signature { get; set; }
    }
}
