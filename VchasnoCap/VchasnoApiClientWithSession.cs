using System;
using NLog;
using System.Threading;
using System.Threading.Tasks;
using VchasnoCapConsole.Data;
using VchasnoCapConsole.Integration.OperationResult;
using VchasnoCapConsole.Integration.ApiClient;
using VchasnoCapConsole.VchasnoCap.Util;

using CreateSessionRequest = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapCreateSessionRequest;
using CreateSessionResponse = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapCreateSessionResponse;
using SessionStatusRequest = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapSessionStatusRequest;
using SessionStatusResponse = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapSessionStatusResponse;
using SessionStatus = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapSessionStatus;
using SessionSignRequest = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapSessionSignRequest;
using SessionSignResponse = VchasnoCapConsole.VchasnoCap.Data.Session.VchasnoCapSessionSignResponse;

namespace VchasnoCapConsole.VchasnoCap
{
    public sealed class VchasnoApiClientWithSession : VchasnoApiClientBase
    {
        private bool _isAuthorizationInProgress = false;

        private string _password;
        private string _authSessionId;
        private string _authSessionToken;

        public VchasnoApiClientWithSession(string apiKey, string clientId, string password, ILogger logger = null) : base(clientId, logger)
        {
            _password = password;
            ApiClient.AddCustomHeader("Authorization", apiKey);
        }

        public async Task<OperationResultInfo> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            if (_isAuthorizationInProgress)
            {
                return OperationResultInfo.CreateError("В процесі");
            }

            try
            {
                _isAuthorizationInProgress = true;

                var request = new CreateSessionRequest { clientId = ClientId };
                var sessionCreateResult = await PerformOperationAsync<CreateSessionRequest, CreateSessionResponse>("api/sessions/create", request);
                if (!sessionCreateResult.IsSuccessful)
                {
                    return OperationResultInfo.CreateError(sessionCreateResult);
                }

                if (sessionCreateResult.Value.errorCode > 0)
                {
                    return OperationResultInfo.CreateError($"Вчасно.КЕП: Операція завершилася з помилкою {sessionCreateResult.Value.GetErrorMessage()}");
                }

                _authSessionId = sessionCreateResult.Value.authSessionId;

                var checkEndTime = DateTime.UtcNow.AddTicks(STATUS_CHECK_DURATION_TICKS);
                OperationResultScalar<SessionStatusResponse> operationResult;
                SessionStatus status;
                do
                {
                    await Task.Delay(CHECK_DELAY_MS);

                    operationResult = await GetSessionStatusAsync();

                    if (!operationResult.IsSuccessful)
                    {
                        return OperationResultInfo.CreateError(operationResult);
                    }

                    status = operationResult.Value.GetStatus();
                }
                while (status == SessionStatus.init && checkEndTime > DateTime.UtcNow && !cancellationToken.IsCancellationRequested);

                if (cancellationToken.IsCancellationRequested)
                {
                    return OperationResultInfo.CreateError("Операція скасована користувачем");
                }

                if (status == SessionStatus.init)
                {
                    return OperationResultInfo.CreateError("Вчасно.КЕП: Перевищено ліміт очікування");
                }

                if (status != SessionStatus.ready)
                {
                    return OperationResultInfo.CreateError($"Вчасно.КЕП: Авторизація завершилася зі статусом: {status}");
                }

                _authSessionToken = operationResult.Value.token;
                ApiClient.AddCustomHeader("X-Cloud-Signer-AuthSession", _authSessionToken);

                return OperationResultInfo.CreateSuccessful();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return OperationResultInfo.CreateError(ex);
            }
            finally
            {
                _isAuthorizationInProgress = false;
            }
        }

        private async Task<OperationResultScalar<SessionStatusResponse>> GetSessionStatusAsync()
        {
            if (string.IsNullOrEmpty(_authSessionId))
            {
                return OperationResultScalar<SessionStatusResponse>.CreateError("Не визначено ідентифікатор сесії");
            }

            var statusRequest = new SessionStatusRequest { authSessionId = _authSessionId };
            var operationResult = await PerformOperationAsync<SessionStatusRequest, SessionStatusResponse>("api/sessions/check", statusRequest);

            if (operationResult.IsSuccessful && operationResult.Value.errorCode > 0)
            {
                return OperationResultScalar<SessionStatusResponse>.CreateError($"Вчасно.КЕП: Операція завершилася з помилкою {operationResult.Value.GetErrorMessage()}");
            }

            return operationResult;
        }

        public async Task<SignedContentResponse> SignAsync(SignContentRequest request, CancellationToken cancellationToken = default)
        {
            var hashResult = request.GatDataHashBase64String();

            if (!hashResult.IsSuccessful)
            {
                return SignedContentResponse.FromError(hashResult);
            }

            var sessionStatusResult = await GetSessionStatusAsync();

            if (!sessionStatusResult.IsSuccessful)
            {
                return SignedContentResponse.FromError(sessionStatusResult);
            }

            var sessionStatus = sessionStatusResult.Value.GetStatus();

            if (sessionStatus == SessionStatus.expired)
            {
                return new SignedContentResponse { IsSuccess = false, StatusMessage = $"Вчасно.КЕП: Час сесії сплинув. Пройдіть авторизацію повторно!" };
            }

            if (sessionStatus != SessionStatus.provided)
            {
                return new SignedContentResponse { IsSuccess = false, StatusMessage = $"Вчасно.КЕП: Статус сесії ({sessionStatus}) не дозволяє виконувати підпис. Пройдіть авторизацію повторно!" };
            }

            var signRequest = new SessionSignRequest
            {
                authSessionToken = _authSessionToken,
                clientId = ClientId,
                hash = hashResult.Value,
                password = _password,
            };

            var signResult = await PerformOperationAsync<SessionSignRequest, SessionSignResponse>("api/sessions/sign-hash", signRequest);

            if (!signResult.IsSuccessful)
            {
                return SignedContentResponse.FromError(signResult);
            }

            if (signResult.Value.errorCode == 2)
            {
                return new SignedContentResponse { IsSuccess = false, StatusMessage = "Вчасно.КЕП: Пароль не вірний" };
            }

            if (signResult.Value.errorCode > 0)
            {
                return new SignedContentResponse { IsSuccess = false, StatusMessage = $"Вчасно.КЕП: Операція завершилася з помилкою {signResult.Value.GetErrorMessage()}" };
            }

            var replacementResult = request.ReplaceHashWithDataInSignature(signResult.Value.signature);

            return SignedContentResponse.From(replacementResult);
        }

        public async Task<SignedContentResponse[]> SignAsync(SignContentRequest[] requests, CancellationToken cancellationToken = default)
        {
            var results = new SignedContentResponse[requests.Length];
            for (var i = 0; i < requests.Length; i++)
            {
                results[i] = await SignAsync(requests[i], cancellationToken);
            }
            return results;
        }
    }
}
