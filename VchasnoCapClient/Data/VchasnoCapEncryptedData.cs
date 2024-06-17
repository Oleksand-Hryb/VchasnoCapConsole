namespace VchasnoCapConsole.VchasnoCapClient.Data
{
    class VchasnoCapEncryptedDataRequest
    {
        public string authData { get; set; }
        public string encryptedData { get; set; }
    }

    class VchasnoCapEncryptedDataResponse : VchasnoCapResponseBase
    {
        public string encryptedData { get; set; }
    }
}
