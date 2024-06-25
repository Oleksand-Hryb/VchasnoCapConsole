using System;
using System.Text;
using VchasnoCapConsole.EUSign;
using VchasnoCapConsole.Integration.OperationResult;

namespace VchasnoCapConsole.Data
{
    public class SignedContentResponse
    {
        public bool IsSuccess { get; set; }
        public string StatusMessage { get; set; }
        public byte[] SignedData { get; set; }

        public override string ToString()
        {
            return this.ToLogString(addSignedData: false);
        }

        public static SignedContentResponse FromError(OperationResultBase result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.IsSuccessful)
            {
                throw new InvalidOperationException();
            }

            return new SignedContentResponse
            {
                IsSuccess = result.IsSuccessful,
                StatusMessage = result.StatusMessage,
            };
        }

        public static SignedContentResponse From(OperationResultScalar<byte[]> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new SignedContentResponse
            {
                IsSuccess = result.IsSuccessful,
                StatusMessage = result.StatusMessage,
                SignedData = result.Value,
            };
        }
    }

    public static class SignedContentResponseUtil
    {
        public static string ToLogString(this SignedContentResponse response, bool addSignedData = true)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            var sb = new StringBuilder();

            sb.Append("IsSuccess: ").Append(response.IsSuccess);

            if (response.IsSuccess)
            {
                sb.Append(", SignedData length: ");

                if (response.SignedData != null)
                {
                    sb.Append(response.SignedData.Length);

                    if (addSignedData)
                    {
                        sb.Append(", SignedData: ").Append(response.GetSignedDataBase64String());
                    }
                }
                else
                {
                    sb.Append("[null]");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(response.StatusMessage))
                {
                    sb.Append(", StatusMessage: ").Append(response.StatusMessage);
                }
            }

            return sb.ToString();
        }

        public static string GetSignedDataBase64String(this SignedContentResponse response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (response.SignedData?.Length > 0 && 
                IEUSignCP.BASE64Encode(response.SignedData, out string base64String) != IEUSignCP.EU_ERROR_NONE)
            {
                return base64String;
            }
            return string.Empty;
        }
    }

}
