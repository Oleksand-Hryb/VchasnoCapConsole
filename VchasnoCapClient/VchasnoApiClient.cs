using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using EUSignCP;
using VchasnoCapConsole.Integration.ApiClient;
using VchasnoCapConsole.Integration.OperationResult;
using VchasnoCapConsole.VchasnoCapClient.Data;

namespace VchasnoCapConsole.VchasnoCapClient
{
    class VchasnoApiClient
    {
        private const int SIGN_CHECK_TIMEOUT = 1000; // ms
        private const int MAX_SIGN_CHECK_TRIES = 600; // ~ 10 minutes

        private byte[] _keyAgreement = null;
        private byte[] _digitalSignature = null;

        private EUSignCPSession _session = null;

        private readonly ApiClient _apiClient;
        private readonly string _clientId;

        public VchasnoApiClient(string clientId)
        {
            if (!IEUSignCP.IsInitialized())
            {
                IEUSignCP.Initialize();
            }

            _apiClient = new ApiClient("https://cs.vchasno.ua/ss");
            _clientId = clientId;
        }

        public async Task<OperationResultInfo> EnsureCertificatesAsync()
        {
            if (_keyAgreement != null && _digitalSignature != null)
            {
                return OperationResultInfo.CreateSuccessful();
            }

            var apiResult = await _apiClient.GetScalarAsync<VchasnoCapCertificatesResponse>("get-certificates");
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

        public async Task<List<OperationResultScalar<byte[]>>> SignAsync(List<(byte[] Data, string DataDescrition)> datas)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResultScalar<byte[]>> SignAsync(byte[] data, string dataDescrition = null)
        {
            var error = IEUSignCP.HashData(data, out string hashData);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            var request = new VchasnoCapSimpleSignRequest
            {
                clientId = _clientId,
                originatorDescription = "Test",
                operationDescription = dataDescrition ?? "Data",
                hash = hashData,
            };

            var acquireResult = await PerformOperationAsync<VchasnoCapSimpleSignRequest, VchasnoCapSimpleSignResponse>("acquire-sign", request);

            if (!acquireResult.IsSuccessful)
            {
                return OperationResultScalar<byte[]>.CreateError(acquireResult);
            }

            var operationId = acquireResult.Value.operationId;

            var signCheckTries = 0;
            int status;
            OperationResultScalar<VchasnoCapOperationStatusResponse> operationResult;
            do
            {
                await Task.Delay(SIGN_CHECK_TIMEOUT);

                var statusRequest = new VchasnoCapOperationStatusRequest { clientId = _clientId, operationId = operationId };
                operationResult = await PerformOperationAsync<VchasnoCapOperationStatusRequest, VchasnoCapOperationStatusResponse>("sign-status", statusRequest);

                if (!operationResult.IsSuccessful)
                {
                    break;
                }

                signCheckTries++;
                status = operationResult.Value.status;
            }
            while (status == 1 && signCheckTries < MAX_SIGN_CHECK_TRIES);

            await ReleaseOperationAsync(operationId);

            if (!operationResult.IsSuccessful)
            {
                return OperationResultScalar<byte[]>.CreateError(operationResult);
            }

            if (operationResult.Value.errorCode != 0)
            {
                return OperationResultScalar<byte[]>.CreateError(operationResult.Value.errorMessage);
            }

            error = IEUSignCP.BASE64Decode(operationResult.Value.signature, out var signatureBytes);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.CreateEmptySign(data, out byte[] emptySign);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.GetSigner(0, signatureBytes, out var signer);

            if (error != IEUSignCP.EU_ERROR_NONE)
            {
                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }

            error = IEUSignCP.GetSignerInfo(0, signatureBytes, out _, out var signerCertificate);

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

        protected async Task<OperationResultInfo> ReleaseOperationAsync(string operationId)
        {
            var releaseRequest = new VchasnoCapReleaseOperationIdRequest { clientId = _clientId, operationId = operationId, };
            var result = await PerformOperationAsync<VchasnoCapReleaseOperationIdRequest, VchasnoCapReleaseOperationIdResponse>("release-operation-id", releaseRequest);
            return OperationResultInfo.From(result);
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

                var encryptedData = new VchasnoCapEncryptedDataRequest
                {
                    authData = clientDataResult.Value,
                    encryptedData = encryptionResult.Value,
                };

                var operationResult = await _apiClient.PostAsync<VchasnoCapEncryptedDataResponse>(operation, encryptedData);

                if (!operationResult.IsSuccessful)
                {
                    return OperationResultScalar<TResponse>.CreateError(operationResult);
                }

                var decryptionResult = _session.DecryptObjectFromBase64<VchasnoCapSignedData>(operationResult.Value.encryptedData);

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
