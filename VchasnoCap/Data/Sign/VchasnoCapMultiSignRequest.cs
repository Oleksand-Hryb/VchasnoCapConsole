using System.Collections.Generic;

namespace VchasnoCapConsole.VchasnoCap.Data.Sign
{
    sealed class VchasnoCapMultiSignRequest : VchasnoCapSignRequestBase
    {
        public List<string> operationDescriptions { get; set; } = new List<string>();
        public List<string> hashes { get; set; } = new List<string>();
    }
}
