using System;
using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VchasnoCapConsole.Data;
using VchasnoCapConsole.Integration.OperationResult;
using VchasnoCapConsole.VchasnoCap.Util;
using VchasnoCapConsole.VchasnoCap.Data.Common;
using VchasnoCapConsole.VchasnoCap.Data.OperationRelease;
using VchasnoCapConsole.VchasnoCap.Data.OperationStatus;
using VchasnoCapConsole.VchasnoCap.Data.Sign;

using SingleSignRequest = VchasnoCapConsole.VchasnoCap.Data.Sign.VchasnoCapSingleSignRequest;
using SingleSignOperationStatusResponse = VchasnoCapConsole.VchasnoCap.Data.OperationStatus.VchasnoCapSingleSignOperationStatusResponse;
using MultiSignRequest = VchasnoCapConsole.VchasnoCap.Data.Sign.VchasnoCapMultiSignRequest;
using MultiSignOperationStatusResponse = VchasnoCapConsole.VchasnoCap.Data.OperationStatus.VchasnoCapMultiSignOperationStatusResponse;

namespace VchasnoCapConsole.VchasnoCap
{
    public sealed class VchasnoApiClient : VchasnoApiClientBase
    {
        private const string DEFAULT_DATA_DESCRIPTION = "Дані для підпису";
        private const int MULTI_SIGN_MAX_DATAS_COUNT = 250;

        public VchasnoApiClient(string clientId, ILogger logger = null) : base(clientId, logger)
        {
        }

        public async Task<SignedContentResponse> SignAsync(SignContentRequest dataToSign, CancellationToken cancellationToken = default)
        {
            if (dataToSign == null)
            {
                throw new ArgumentNullException(nameof(dataToSign));
            }

            var dataHashResult = dataToSign.GatDataHashBase64String();
            if (!dataHashResult.IsSuccessful)
            {
                return SignedContentResponse.From(OperationResultScalar<byte[]>.CreateError(dataHashResult));
            }

            var request = CreateBaseRequest<SingleSignRequest>();
            request.operationDescription = dataToSign.Description ?? DEFAULT_DATA_DESCRIPTION;
            request.hash = dataHashResult.Value;

            var signResult = await InternalSignAsync<SingleSignRequest, SingleSignOperationStatusResponse>(request, cancellationToken);

            if (!signResult.IsSuccessful)
            {
                return SignedContentResponse.From(OperationResultScalar<byte[]>.CreateError(signResult));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return SignedContentResponse.From(OperationResultScalar<byte[]>.CreateError("Операція скасована"));
            }

            var result = dataToSign.ReplaceHashWithDataInSignature(signResult.Value.signature);
            return SignedContentResponse.From(result);
        }

        public async Task<SignedContentResponse[]> SignAsync(SignContentRequest[] datasToSign, CancellationToken cancellationToken = default)
        {
            if (datasToSign == null)
            {
                throw new ArgumentNullException(nameof(datasToSign));
            }

            if (datasToSign.Length > MULTI_SIGN_MAX_DATAS_COUNT)
            {
                throw new ArgumentException(nameof(datasToSign), $"{datasToSign} maximum length is {MULTI_SIGN_MAX_DATAS_COUNT} items!");
            }

            var results = new SignedContentResponse[datasToSign.Length];

            if (datasToSign.Length == 0)
            {
                return new SignedContentResponse[0];
            }

            if (datasToSign.Length == 1)
            {
                results[0] = await SignAsync(datasToSign[0], cancellationToken);
                return results;
            }

            var hashIndexes = new Dictionary<string, List<int>>();

            var request = CreateBaseRequest<MultiSignRequest>();

            for (var i = 0; i < datasToSign.Length; i++)
            {
                var dataToSign = datasToSign[i];
                var hashResult = dataToSign.GatDataHashBase64String();
                if (hashResult.IsSuccessful)
                {
                    var hash = hashResult.Value;
                    if (!hashIndexes.ContainsKey(hash)) hashIndexes[hash] = new List<int>();
                    hashIndexes[hashResult.Value].Add(i);

                    if (!request.hashes.Contains(hash))
                    {
                        request.hashes.Add(hashResult.Value);
                        request.operationDescriptions.Add(dataToSign.Description ?? DEFAULT_DATA_DESCRIPTION);
                    }

                    results[i] = new SignedContentResponse { IsSuccess = false, StatusMessage = "Не оброблено" };
                }
                else
                {
                    results[i] = SignedContentResponse.FromError(hashResult);
                }
            }

            var signResult = await InternalSignAsync<MultiSignRequest, MultiSignOperationStatusResponse>(request, cancellationToken);

            if (!signResult.IsSuccessful)
            {
                foreach (var i in hashIndexes.SelectMany(_ => _.Value))
                {
                    results[i] = SignedContentResponse.FromError(signResult);
                }
                return results;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                foreach (var i in hashIndexes.SelectMany(_ => _.Value))
                {
                    results[i] = new SignedContentResponse { IsSuccess = false, StatusMessage = "Операція скасована користувачем" };
                }
                return results;
            }

            foreach (var signature in signResult.Value.signatures)
            {
                if (hashIndexes.TryGetValue(signature.hash, out var indexes))
                {
                    SignedContentResponse result;

                    if (signature.errorCode == 0)
                    {
                        var data = datasToSign.ElementAtOrDefault(indexes.First());
                        if (data != null)
                        {
                            var r = data.ReplaceHashWithDataInSignature(signature.signature);
                            result = SignedContentResponse.From(r);
                        }
                        else
                        {
                            result = new SignedContentResponse { IsSuccess = false, StatusMessage = "Упс. Щось пішло не так!" };
                        }
                    }
                    else
                    {
                        result = new SignedContentResponse { IsSuccess = false, StatusMessage = signature.errorMessage };
                    }

                    foreach (var index in indexes) results[index] = result;
                }
            }

            return results;
        }

        private T CreateBaseRequest<T>() where T : VchasnoCapSignRequestBase, new()
        {
            return new T
            {
                clientId = ClientId,
                originatorDescription = "DocDream",
            };
        }

        private async Task<OperationResultScalar<TOperationStatusResponse>> InternalSignAsync<TSignRequest, TOperationStatusResponse>(TSignRequest signRequest, CancellationToken cancellationToken = default) where TOperationStatusResponse : VchasnoCapResponseBase
        {
            try
            {
                var acquireResult = await PerformOperationAsync<TSignRequest, VchasnoCapSignResponse>("ss/acquire-sign", signRequest);

                if (!acquireResult.IsSuccessful)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError(acquireResult);
                }

                if (acquireResult.Value.errorCode != 0 || acquireResult.Value.status == 4)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError($"Вчасно.КЕП: Операція завершилася з помилкою {acquireResult.Value.GetErrorMessage()}");
                }

                var operationId = acquireResult.Value.operationId;

                var signCheckEndTime = DateTime.UtcNow.AddTicks(STATUS_CHECK_DURATION_TICKS);
                OperationResultScalar<TOperationStatusResponse> operationResult;
                do
                {
                    await Task.Delay(CHECK_DELAY_MS);

                    var statusRequest = new VchasnoCapOperationStatusRequest { clientId = ClientId, operationId = operationId };
                    operationResult = await PerformOperationAsync<VchasnoCapOperationStatusRequest, TOperationStatusResponse>("ss/sign-status", statusRequest);

                    if (!operationResult.IsSuccessful)
                    {
                        break;
                    }
                }
                while (operationResult.Value.status < 2 && signCheckEndTime > DateTime.UtcNow && !cancellationToken.IsCancellationRequested);

                await ReleaseOperationAsync(operationId);

                if (cancellationToken.IsCancellationRequested)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError("Операція скасована користувачем");
                }

                if (!operationResult.IsSuccessful)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError(operationResult);
                }

                if (operationResult.Value.errorCode != 0 || operationResult.Value.status == 4)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError($"Вчасно.КЕП: Операція завершилася з помилкою {operationResult.Value.GetErrorMessage()}");
                }

                if (operationResult.Value.status == 3)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError("Вчасно.КЕП: Операція відхилена");
                }

                if (operationResult.Value.status != 2)
                {
                    return OperationResultScalar<TOperationStatusResponse>.CreateError("Вчасно.КЕП: Перевищено ліміт очікування");
                }

                return operationResult;
            }
            catch (Exception e)
            {
                return OperationResultScalar<TOperationStatusResponse>.CreateError(e);
            }
        }

        private async Task<OperationResultInfo> ReleaseOperationAsync(string operationId)
        {
            var releaseRequest = new VchasnoCapReleaseOperationRequest { clientId = ClientId, operationId = operationId, };
            var result = await PerformOperationAsync<VchasnoCapReleaseOperationRequest, VchasnoCapReleaseOperationResponse>("ss/release-operation-id", releaseRequest);
            return OperationResultInfo.From(result);
        }
    }
}
