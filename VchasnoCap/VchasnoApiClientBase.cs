using System;
using NLog;
using System.Text.Json;
using System.Threading.Tasks;
using VchasnoCapConsole.EUSign;
using VchasnoCapConsole.Integration.ApiClient;
using VchasnoCapConsole.Integration.OperationResult;
using VchasnoCapConsole.VchasnoCap.Util;
using CertificatesResponse = VchasnoCapConsole.VchasnoCap.Data.Common.VchasnoCapCertificatesResponse;
using EncryptedDataRequest = VchasnoCapConsole.VchasnoCap.Data.Common.VchasnoCapEncryptedDataRequest;
using EncryptedDataResponse = VchasnoCapConsole.VchasnoCap.Data.Common.VchasnoCapEncryptedDataResponse;
using SignedData = VchasnoCapConsole.VchasnoCap.Data.Common.VchasnoCapSignedData;

namespace VchasnoCapConsole.VchasnoCap
{
    public abstract class VchasnoApiClientBase
    {
        private const string API_URL = "https://cs.vchasno.ua/";

        protected const short CHECK_DELAY_MS = 1000;
        protected const long STATUS_CHECK_DURATION_TICKS = 5 * TimeSpan.TicksPerMinute;

        private byte[] _keyAgreement = null;
        private byte[] _digitalSignature = null;

        private EUSignCPSession _session = null;

        protected readonly ApiClient ApiClient;
        protected readonly string ClientId;

        protected readonly ILogger Logger;

        protected VchasnoApiClientBase(string clientId, ILogger logger = null)
        {
            InitializeCryptoLab();
            Logger = logger ?? LogManager.GetLogger("Integration.VchasnoApi");
            ApiClient = new ApiClient(API_URL, Logger);
            ClientId = clientId;
        }

        private void InitializeCryptoLab()
        {
            if (IEUSignCP.IsInitialized()) return;

            var initError = IEUSignCP.Initialize();
            if (initError != IEUSignCP.EU_ERROR_NONE)
            {
                throw new ApplicationException(IEUSignCP.GetErrorDesc(initError));
            }
        }

        private async Task<OperationResultInfo> EnsureCertificatesAsync()
        {
            if (_keyAgreement != null && _digitalSignature != null)
            {
                return OperationResultInfo.CreateSuccessful();
            }

            var apiResult = await ApiClient.GetScalarAsync<CertificatesResponse>("ss/get-certificates");
            if (!apiResult.IsSuccessful)
            {
                return OperationResultInfo.CreateError(apiResult);
            }

            var keyAgreementBase64 = apiResult.Value.certificates[0];
            _keyAgreement = Convert.FromBase64String(keyAgreementBase64);
            var digitalSignatureBase64 = apiResult.Value.certificates[1];
            _digitalSignature = Convert.FromBase64String(digitalSignatureBase64);

            return OperationResultInfo.CreateSuccessful();
        }

        private async Task<OperationResultInfo> EnsureSessionAsync()
        {
            var ensureCertificatesResult = await EnsureCertificatesAsync();

            if (!ensureCertificatesResult.IsSuccessful)
            {
                return ensureCertificatesResult;
            }

            if (_session == null || !_session.IsValid())
            {
                if (_session != null)
                {
                    _session.Destroy();
                }

                var result = EUSignCPSession.Create(_keyAgreement);

                if (!result.IsSuccessful)
                {
                    _session = null;
                    return OperationResultInfo.CreateError(result);
                }

                _session = result.Value;
            }

            return OperationResultInfo.CreateSuccessful();
        }

        protected async Task<OperationResultScalar<TResponse>> PerformOperationAsync<TRequest, TResponse>(string operation, TRequest request) where TResponse : class
        {
            try
            {
                var ensureSessionResult = await EnsureSessionAsync();

                if (!ensureSessionResult.IsSuccessful)
                {
                    return OperationResultScalar<TResponse>.CreateError(ensureSessionResult);
                }

                var clientDataResult = _session.GetClientDataBase64();

                if (!clientDataResult.IsSuccessful)
                {
                    return OperationResultScalar<TResponse>.CreateError(clientDataResult);
                }

                var encryptionResult = _session.EncryptObjectToBase64(request);

                if (!encryptionResult.IsSuccessful)
                {
                    return OperationResultScalar<TResponse>.CreateError(encryptionResult);
                }

                var encryptedData = new EncryptedDataRequest
                {
                    authData = clientDataResult.Value,
                    encryptedData = encryptionResult.Value,
                };

                var operationResult = await ApiClient.PostAsync<EncryptedDataResponse>(operation, encryptedData);

                if (!operationResult.IsSuccessful)
                {
                    return OperationResultScalar<TResponse>.CreateError(operationResult);
                }

                if (operationResult.Value.errorCode > 0)
                {
                    return OperationResultScalar<TResponse>.CreateError($"Вчасно.КЕП: Операція завершилася з помилкою {operationResult.Value.GetErrorMessage()}");
                }

                var decryptionResult = _session.DecryptObjectFromBase64<SignedData>(operationResult.Value.encryptedData);

                if (!decryptionResult.IsSuccessful)
                {
                    return OperationResultScalar<TResponse>.CreateError(decryptionResult);
                }

                var error = IEUSignCP.BASE64Decode(decryptionResult.Value.signedData, out var signedDataBytes);

                if (error != IEUSignCP.EU_ERROR_NONE)
                {
                    return OperationResultScalar<TResponse>.CreateEUSignError(error);
                }

                error = IEUSignCP.VerifyDataInternal(signedDataBytes, out var unsignedDataBytes, out _);

                if (error != IEUSignCP.EU_ERROR_NONE)
                {
                    return OperationResultScalar<TResponse>.CreateEUSignError(error);
                }

                Logger.Trace($"Operation unsigned response: {Convert.ToBase64String(unsignedDataBytes)}");

                var response = JsonSerializer.Deserialize<TResponse>(unsignedDataBytes);

                return OperationResultScalar<TResponse>.CreateSuccessful(response);
            }
            catch (Exception e)
            {
                return OperationResultScalar<TResponse>.CreateError(e);
            }
        }
    }
}
