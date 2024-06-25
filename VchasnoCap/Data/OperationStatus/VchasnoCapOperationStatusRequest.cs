namespace VchasnoCapConsole.VchasnoCap.Data.OperationStatus
{
    sealed class VchasnoCapOperationStatusRequest
    {
        public string clientId { get; set; }
        public string operationId { get; set; }
    }
}
