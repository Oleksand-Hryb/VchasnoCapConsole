namespace VchasnoCapConsole.VchasnoCapClient.Data
{
    class VchasnoCapReleaseOperationIdRequest
    {
        public string clientId { get; set; }
        public string operationId { get; set; }
    }

    class VchasnoCapReleaseOperationIdResponse : VchasnoCapResponseBase
    {
        public int status { get; set; }
    }
}
