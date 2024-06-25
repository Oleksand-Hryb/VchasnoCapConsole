using System;
using VchasnoCapConsole.VchasnoCap.Data.Common;
using VchasnoCapConsole.VchasnoCap.Data.Session;

namespace VchasnoCapConsole.VchasnoCap.Util
{
    internal static class VchasnoCapResponseUtil
    {
        internal static string GetErrorMessage(this VchasnoCapResponseBase response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.errorCode == 0) return string.Empty;

            return response.errorMessage ?? VchasnoCapErrorUtil.GetErrorCodeMessage(response.errorCode);
        }

        internal static string GetErrorMessage(this VchasnoCapSessionResponseBase response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.errorCode == 0) return string.Empty;

            return response.errorMessage ?? VchasnoCapErrorUtil.GetErrorCodeMessage(response.errorCode);
        }
    }
}
