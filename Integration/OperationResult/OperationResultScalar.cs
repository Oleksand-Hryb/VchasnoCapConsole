using EUSignCP;
using System;
using System.Collections.Generic;

namespace VchasnoCapConsole.Integration.OperationResult
{
    class OperationResultScalar<TDto> : OperationResultBase
    {
        public TDto Value { get; set; }

        public static OperationResultScalar<TDto> CreateSuccessful(TDto value)
        {
            return new OperationResultScalar<TDto> { Status = OperationStatus.Success, Value = value };
        }

        public static OperationResultScalar<TDto> CreateError(string message, List<string> details = null)
        {
            return CreateError<OperationResultScalar<TDto>>(message, details);
        }

        public static OperationResultScalar<TDto> CreateError<T>(T result) where T : OperationResultBase
        {
            return CreateError<OperationResultScalar<TDto>, T>(result);
        }

        public static OperationResultScalar<TDto> CreateError(Exception e, string additionalMessage = null)
        {
            return CreateError<OperationResultScalar<TDto>>(e, additionalMessage);
        }

        public static OperationResultScalar<TDto> CreateEUSignError(int e, List<string> details = null)
        {
            return CreateError<OperationResultScalar<TDto>>(IEUSignCP.GetErrorDesc(e), details);
        }
    }
}
