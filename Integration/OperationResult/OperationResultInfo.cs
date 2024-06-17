using System;
using System.Collections.Generic;

namespace VchasnoCapConsole.Integration.OperationResult
{
    class OperationResultInfo : OperationResultBase
    {
        public static OperationResultInfo CreateSuccessful()
        {
            return new OperationResultInfo { Status = OperationStatus.Success };
        }

        public static OperationResultInfo CreateError(string message, List<string> details = null)
        {
            return CreateError<OperationResultInfo>(message, details);
        }

        public static OperationResultInfo CreateError<T>(T result) where T : OperationResultBase
        {
            return CreateError<OperationResultInfo, T>(result);
        }

        public static OperationResultInfo CreateError(Exception e, string additionalMessage = null)
        {
            return CreateError<OperationResultInfo>(e, additionalMessage);
        }

        public static OperationResultInfo From<T>(T result) where T : OperationResultBase
        {
            if (result.IsSuccessful) return CreateSuccessful();
            return CreateError(result);
        }
    }
}
