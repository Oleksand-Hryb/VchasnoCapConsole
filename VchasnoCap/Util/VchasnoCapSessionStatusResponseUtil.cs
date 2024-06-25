using System;
using VchasnoCapConsole.VchasnoCap.Data.Session;

namespace VchasnoCapConsole.VchasnoCap.Util
{
    internal static class VchasnoCapSessionStatusResponseUtil
    {
        internal static VchasnoCapSessionStatus GetStatus(this VchasnoCapSessionStatusResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (Enum.TryParse<VchasnoCapSessionStatus>(response.status, out var status))
            {
                return status;
            }
            return VchasnoCapSessionStatus.failed;
        }
    }
}
