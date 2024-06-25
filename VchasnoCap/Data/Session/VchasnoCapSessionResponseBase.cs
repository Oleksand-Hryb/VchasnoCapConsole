using VchasnoCapConsole.VchasnoCap.Util;

namespace VchasnoCapConsole.VchasnoCap.Data.Session
{
    abstract class VchasnoCapSessionResponseBase
    {
        /// <summary>
        /// For more details see: <see cref="VchasnoCapErrorUtil.GetErrorCodeMessage(int?)"/>
        /// </summary>
        public int? errorCode { get; set; }
        public string errorMessage { get; set; }
    }
}
