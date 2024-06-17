using System;
using System.Collections.Generic;

namespace VchasnoCapConsole.Integration.OperationResult
{
    abstract class OperationResultBase
    {
        public OperationStatus Status { get; set; }

        public string StatusMessage { get; set; }

        public List<string> StatusDetails { get; set; }

        public bool IsSuccessful => Status == OperationStatus.Success;

        protected static T CreateError<T, TOther>(TOther otherResult) where T : OperationResultBase, new() where TOther : OperationResultBase
        {
            if (otherResult == null) return null;
            return new T { Status = OperationStatus.Error, StatusMessage = otherResult.StatusMessage, StatusDetails = otherResult.StatusDetails, };
        }

        protected static T CreateError<T>(string message, List<string> details = null) where T : OperationResultBase, new()
        {
            return new T { Status = OperationStatus.Error, StatusMessage = message, StatusDetails = details, };
        }

        protected static T CreateError<T>(Exception e, string additionalMessage = null) where T : OperationResultBase, new()
        {
            var message = additionalMessage == null ? e.Message : $"{additionalMessage}\r\n{e.Message}";
            return new T { Status = OperationStatus.Error, StatusMessage = message };
        }
    }
}
