using System;
using System.Linq;
using System.Text;
using VchasnoCapConsole.EUSign;
using VchasnoCapConsole.Integration.OperationResult;

namespace VchasnoCapConsole.Data
{
    public class SignContentRequest
    {
        public byte[] ContentToSign { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return this.ToLogString(addContent: false);
        }
    }

    public static class SignContentRequestUtil
    {
        public static string ToLogString(this SignContentRequest request, bool addContent = true)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(request.Description))
            {
                sb.Append(request.Description).Append(", ");
            }

            sb.Append("Content length: ");

            if (request.ContentToSign != null)
            {
                sb.Append(request.ContentToSign.Length);

                if (addContent)
                {
                    sb.Append(", Content: ").Append(request.GetContentToSignBase64String());
                }
            }
            else
            {
                sb.Append("[null]");
            }

            return sb.ToString();
        }


        public static string GetContentToSignBase64String(this SignContentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.ContentToSign?.Length > 0 &&
                IEUSignCP.BASE64Encode(request.ContentToSign, out string base64String) != IEUSignCP.EU_ERROR_NONE)
            {
                return base64String;
            }
            return string.Empty;
        }

        public static OperationResultScalar<string> GatDataHashBase64String(this SignContentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var error = IEUSignCP.HashData(request.ContentToSign, out string hashData);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<string>.CreateEUSignError(error);
            }
            return OperationResultScalar<string>.CreateSuccessful(hashData);
        }

        public static OperationResultScalar<byte[]> ReplaceHashWithDataInSignature(this SignContentRequest request, string signedHashBase64)
        {
            if (signedHashBase64 == null) throw new ArgumentNullException(nameof(signedHashBase64));

            var error = IEUSignCP.BASE64Decode(signedHashBase64, out var signedHashBytes);
            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            return request.ReplaceHashWithDataInSignature(signedHashBytes);
        }

        public static OperationResultScalar<byte[]> ReplaceHashWithDataInSignature(this SignContentRequest request, byte[] signedHashBytes)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (signedHashBytes == null) throw new ArgumentNullException(nameof(signedHashBytes));

            var dataBytesToReplaceHash = request.ContentToSign;

            var error = IEUSignCP.GetDataHashFromSignedData(0, signedHashBytes, out byte[] hashFromSignBytes);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.HashData(dataBytesToReplaceHash, out byte[] dataHashBytes);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            if (!hashFromSignBytes.SequenceEqual(dataHashBytes))
            {
                return OperationResultScalar<byte[]>.CreateError("Геш у підписі не відповідає даним якими потрібно його замінити!");
            }

            error = IEUSignCP.CreateEmptySign(dataBytesToReplaceHash, out byte[] emptySign);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.GetSigner(0, signedHashBytes, out var signer);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.GetSignerInfo(0, signedHashBytes, out _, out var signerCertificate);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.AppendSigner(signer, signerCertificate, emptySign, out var sign);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            return OperationResultScalar<byte[]>.CreateSuccessful(sign);
        }
    }
}
