using VchasnoCapConsole.VchasnoCap.Data.Common;

namespace VchasnoCapConsole.VchasnoCap.Data.Sign
{
    sealed class VchasnoCapSignResult : VchasnoCapResponseBase
    {
        public string hash { get; set; }
        public string signature { get; set; }
    }
}
