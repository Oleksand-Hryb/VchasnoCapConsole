using System.Collections.Generic;
using VchasnoCapConsole.VchasnoCap.Data.Common;
using VchasnoCapConsole.VchasnoCap.Data.Sign;

namespace VchasnoCapConsole.VchasnoCap.Data.OperationStatus
{
    sealed class VchasnoCapMultiSignOperationStatusResponse : VchasnoCapResponseBase
    {
        public List<VchasnoCapSignResult> signatures { get; set; }
    }
}
