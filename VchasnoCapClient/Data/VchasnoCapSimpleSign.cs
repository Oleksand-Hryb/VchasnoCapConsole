namespace VchasnoCapConsole.VchasnoCapClient.Data
{
    class VchasnoCapSimpleSignRequest
    {
        public string clientId { get; set; }
        public string originatorDescription { get; set; }
        public string operationDescription { get; set; }
        public string hash { get; set; }
    }

    class VchasnoCapSimpleSignResponse : VchasnoCapResponseBase
    {
        public string operationId { get; set; }
        public int status { get; set; }
    }
}
