using VchasnoCapConsole.VchasnoCap.Util;

namespace VchasnoCapConsole.VchasnoCap.Data.Common
{
    abstract class VchasnoCapResponseBase
    {
        /// <summary>
        /// <list type="table">
        /// <item>0 - операція зареєстрована</item>
        /// <item>1 - операція розпочата</item>
        /// <item>2 - операція виконана</item>
        /// <item>3 - операція відхилена</item>
        /// <item>4 - операція завершилася з помилкою</item>
        /// </list>
        /// </summary>
        public int status { get; set; }

        /// <summary>
        /// For more details see: <see cref="VchasnoCapErrorUtil.GetErrorCodeMessage(int?)"/>
        /// </summary>
        public int errorCode { get; set; }
        public string errorMessage { get; set; }
    }
}
