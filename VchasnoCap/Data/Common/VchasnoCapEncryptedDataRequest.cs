namespace VchasnoCapConsole.VchasnoCap.Data.Common
{
    sealed class VchasnoCapEncryptedDataRequest
    {
        public string authData { get; set; }
        public string encryptedData { get; set; }
    }
}
