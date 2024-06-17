namespace VchasnoCapConsole.VchasnoCapClient.Data
{
    class VchasnoCapOperationStatusRequest
    {
        public string clientId { get; set; }
        public string operationId { get; set; }
    }

    class VchasnoCapOperationStatusResponse : VchasnoCapResponseBase
    {
        public int status { get; set; }
        public string signature { get; set; }
    }
}
