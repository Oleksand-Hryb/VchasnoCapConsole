using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VchasnoCapConsole.Integration.ApiClient.Enums;
using VchasnoCapConsole.Integration.OperationResult;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http;

namespace VchasnoCapConsole.Integration.ApiClient
{
    public class ApiClient
    {
        protected static HttpClient HttpClient = new HttpClient();

        protected ILogger Logger;

        protected string BaseAddress = null;
        protected ApiClientAuthenticationType AuthenticationType;

        protected Dictionary<string, List<string>> AdditionalHeaders { get; } = new Dictionary<string, List<string>>();

        public ApiClient(ILogger logger = null)
        {
            Logger = logger ?? LogManager.GetLogger($"Integration.{GetType().Name}");
        }

        public ApiClient(string baseAddress, ILogger logger = null) : this(logger)
        {
            ChangeBaseAddress(baseAddress);
        }

        public ApiClient(string baseAddress, TimeSpan timeout, ILogger logger = null) : this(baseAddress, logger)
        {
            HttpClient = new HttpClient()
            {
                Timeout = timeout,
            };
        }

        public void ChangeBaseAddress(string baseAddress)
        {
            BaseAddress = baseAddress?.TrimEnd('/');
        }

        #region Authorization

        #region BasicHttp

        protected string Username;
        protected string Password;

        public void SetBasicAuthentication(string userName, string password)
        {
            Username = userName;
            Password = password;
            AuthenticationType = ApiClientAuthenticationType.BasicHttp;
        }

        #endregion

        #region Token

        protected string AccessToken;

        public void SetTokenAuthentication(string token)
        {
            AccessToken = token;
            AuthenticationType = ApiClientAuthenticationType.Token;
        }

        #endregion

        public ApiClientAuthenticationType GetCurrentAuthenticationType() => AuthenticationType;

        #endregion

        #region Headers

        public ApiClient AddCustomHeader(string key, string value, bool isMultiValueAllowed = true)
        {
            if (!AdditionalHeaders.ContainsKey(key))
            {
                AdditionalHeaders.Add(key, new List<string>());
            }

            if (!isMultiValueAllowed)
            {
                AdditionalHeaders[key].Clear();
            }

            AdditionalHeaders[key].Add(value);

            return this;
        }

        public ApiClient AddApiKeyHeader(string value) => AddCustomHeader("API-Key", value, false);

        public ApiClient RemoveCustomHeader(string key)
        {
            if (AdditionalHeaders.ContainsKey(key))
            {
                AdditionalHeaders.Remove(key);
            }
            return this;
        }

        #endregion

        #region Get method

        public Task<OperationResultScalar<TResult>> GetScalarAsync<TResult>(string operation, string comment = null) where TResult : class
        {
            return PerformOperationAsync<HttpContent, TResult>(ApiClientHttpMethod.GET, operation, null, comment);
        }

        #endregion

        #region Post methods

        public Task<OperationResultScalar<TResult>> PostAsync<TResult>(string operation, string argument, string contentType, string comment = null) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.POST, operation, argument, contentType, comment);
        }

        public Task<OperationResultScalar<TResult>> PostAsync<TResult>(string operation, object argument, string comment = null) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.POST, operation, argument, comment);
        }

        public Task<OperationResultScalar<TResult>> PostAsync<TContent, TResult>(string operation, TContent content, string comment = null) where TResult : class where TContent : HttpContent
        {
            return PerformOperationAsync<TContent, TResult>(ApiClientHttpMethod.POST, operation, content, comment);
        }

        #endregion

        #region Put methods

        public Task<OperationResultScalar<TResult>> PutAsync<TResult>(string operation, string argument, string contentType) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.PUT, operation, argument, contentType);
        }

        public Task<OperationResultScalar<TResult>> PutAsync<TResult>(string operation, object argument) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.PUT, operation, argument);
        }

        public Task<OperationResultScalar<TResult>> PutAsync<TContent, TResult>(string operation, TContent content) where TResult : class where TContent : HttpContent
        {
            return PerformOperationAsync<TContent, TResult>(ApiClientHttpMethod.PUT, operation, content);
        }

        #endregion

        #region Patch methods

        public Task<OperationResultScalar<TResult>> PatchAsync<TResult>(string operation, string argument, string contentType, string comment = null) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.PATCH, operation, argument, contentType, comment);
        }

        public Task<OperationResultScalar<TResult>> PatchAsync<TResult>(string operation, object argument, string comment = null) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.PATCH, operation, argument, comment);
        }

        public Task<OperationResultScalar<TResult>> PatchAsync<TContent, TResult>(string operation, TContent content, string comment = null) where TResult : class where TContent : HttpContent
        {
            return PerformOperationAsync<TContent, TResult>(ApiClientHttpMethod.PATCH, operation, content, comment);
        }

        #endregion

        #region Delete methods

        public Task<OperationResultScalar<TResult>> DeleteAsync<TResult>(string operation, string comment = null) where TResult : class
        {
            return PerformOperationAsync<HttpContent, TResult>(ApiClientHttpMethod.DELETE, operation, null, comment);
        }

        public Task<OperationResultScalar<TResult>> DeleteAsync<TResult>(string operation, object argument, string comment = null) where TResult : class
        {
            return PerformOperationAsync<TResult>(ApiClientHttpMethod.DELETE, operation, argument, comment);
        }

        #endregion

        #region Perform operation methods

        public Task<OperationResultScalar<TResult>> PerformOperationAsync<TResult>(ApiClientHttpMethod method, string operation, string argument, string contentType, string comment = null) where TResult : class
        {
            var content = new StringContent(argument, Encoding.UTF8, contentType);
            return PerformOperationAsync<StringContent, TResult>(method, operation, content, comment);
        }

        public Task<OperationResultScalar<TResult>> PerformOperationAsync<TResult>(ApiClientHttpMethod method, string operation, object argument = null, string comment = null) where TResult : class
        {

            var argumentJson = JsonSerializer.Serialize(argument);
            var content = new StringContent(argumentJson, Encoding.UTF8, "application/json");
            return PerformOperationAsync<StringContent, TResult>(method, operation, content, comment);
        }

        public virtual async Task<OperationResultScalar<TResult>> PerformOperationAsync<TContent, TResult>(ApiClientHttpMethod method, string operation, TContent content, string comment = null) where TResult : class where TContent : HttpContent
        {
            var startTicks = DateTime.Now.Ticks;
            try
            {
                var requestUrl = ComposeUri(operation);
                if (string.IsNullOrEmpty(requestUrl))
                {
                    return OperationResultScalar<TResult>.CreateError("Url is empty");
                }

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(requestUrl),
                    Method = new HttpMethod(method.ToString()),
                };

                var operationId = Math.Abs(Guid.NewGuid().GetHashCode());

                Logger.Trace($"{method} [{operationId}] -> {requestUrl} {(comment != null ? $" ({comment})" : "")}");

                var authentication = CreateAuthorizationHeader();
                if (authentication != null)
                {
                    request.Headers.Authorization = authentication;
                }


                foreach (var header in AdditionalHeaders.Where(e => e.Value.Count > 0))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (content != null && method.HasContent())
                {
                    request.Content = content;
                    if (content is StringContent sc)
                    {
                        var contentString = await sc.ReadAsStringAsync();
                        Logger.Trace($"[{operationId}] Content (length {content.Headers.ContentLength}): {contentString}");
                    }
                    else
                    {
                        Logger.Trace($"[{operationId}] Content length {content.Headers.ContentLength} of type {content.Headers.ContentType}");
                    }
                }

#if DEBUG
                if (request.Headers.Count() > 0)
                {
                    var headers = request.Headers.SelectMany(h => h.Value.Select(v => $"'{h.Key}':'{v}'")).ToArray();
                    if (headers.Length > 0)
                    {
                        Logger.Trace($"[{operationId}] Headers: {string.Join(", ", headers)}");
                    }
                }
#endif

                var response = await HttpClient.SendAsync(request);

                string responseStringContent;
                List<string> errorDetails;

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(TResult) == typeof(byte[]))
                    {
                        Logger.Trace($"Response content [{operationId}]: MIME type {response.Content.Headers.ContentType} Length {response.Content.Headers.ContentLength}");
                        var byteArrayContent = await response.Content.ReadAsByteArrayAsync();
                        return OperationResultScalar<TResult>.CreateSuccessful(byteArrayContent as TResult);
                    }

                    responseStringContent = await response.Content.ReadAsStringAsync();
                    Logger.Trace($"Response content (length {response.Content.Headers.ContentLength}) [{operationId}]: {responseStringContent}");

                    if (typeof(TResult) == typeof(string))
                    {
                        return OperationResultScalar<TResult>.CreateSuccessful(responseStringContent as TResult);
                    }

                    TResult result;
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        result = await JsonSerializer.DeserializeAsync<TResult>(stream);
                    }

                    if (result != null)
                    {
                        return OperationResultScalar<TResult>.CreateSuccessful(result);
                    }

                    Logger.Error($"Could not deserialize server response [{operationId}]: {responseStringContent}");
                    errorDetails = !string.IsNullOrWhiteSpace(responseStringContent) ? new List<string> { responseStringContent } : null;
                    return OperationResultScalar<TResult>.CreateError("Could not deserialize message", details: errorDetails);
                }
                else
                {
                    responseStringContent = await response.Content.ReadAsStringAsync();
                    Logger.Error($"ERROR [{operationId}] : {response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{responseStringContent}");
                    errorDetails = !string.IsNullOrWhiteSpace(responseStringContent) ? new List<string> { responseStringContent } : null;
                    return OperationResultScalar<TResult>.CreateError(response.ReasonPhrase, details: errorDetails);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return OperationResultScalar<TResult>.CreateError(e);
            }
        }

        #endregion

        #region Utils

        private AuthenticationHeaderValue CreateAuthorizationHeader()
        {
            switch (AuthenticationType)
            {
                case ApiClientAuthenticationType.BasicHttp:
                    var encodedUserPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
                    return new AuthenticationHeaderValue("Basic", encodedUserPassword);
                case ApiClientAuthenticationType.Token:
                    return new AuthenticationHeaderValue("Bearer", AccessToken);
                default:
                    return null;
            }
        }

        private string ComposeUri(string operation)
        {
            var uriBuilder = new StringBuilder((BaseAddress?.Length ?? 0) + (operation?.Length ?? 0) + 1);

            if (!string.IsNullOrWhiteSpace(BaseAddress))
            {
                uriBuilder.Append(BaseAddress);
            }

            if (!string.IsNullOrWhiteSpace(BaseAddress) && !string.IsNullOrWhiteSpace(operation))
            {
                uriBuilder.Append('/');
            }

            if (!string.IsNullOrWhiteSpace(operation))
            {
                uriBuilder.Append(operation.TrimStart('/'));
            }

            return uriBuilder.ToString();
        }

        #endregion
    }
}
