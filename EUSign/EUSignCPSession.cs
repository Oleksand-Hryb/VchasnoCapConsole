using System;
using System.Text.Json;
using VchasnoCapConsole.Integration.OperationResult;

namespace VchasnoCapConsole.EUSign
{
    class EUSignCPSession
    {
        public static int SessionExpireTimeInSecondsDelta = 10 * 60; // 10 minutes
        public static int SessionExpireTimeInSeconds = 24 * 60 * 60; // 24 hour

        private IntPtr _session;
        private byte[] _clientData;

        public DateTime ValidTo { get; private set; }

        private EUSignCPSession(IntPtr session, byte[] clientData, DateTime validTo)
        {
            _session = session;
            _clientData = clientData;
            ValidTo = validTo;
        }

        public bool IsValid() => GetSession().IsSuccessful;

        public OperationResultScalar<IntPtr> GetSession()
        {
            if (ValidTo <= DateTime.UtcNow)
            {
                return OperationResultScalar<IntPtr>.CreateError("Session is expired!");
            }

            if (_session == default)
            {
                return OperationResultScalar<IntPtr>.CreateError("Session is destroyed or not inited!");
            }

            return OperationResultScalar<IntPtr>.CreateSuccessful(_session);
        }

        #region ClientData

        public OperationResultScalar<byte[]> GetClientData()
        {
            if (ValidTo <= DateTime.UtcNow)
            {
                return OperationResultScalar<byte[]>.CreateError("Session is expired!");
            }

            if (_clientData == null)
            {
                return OperationResultScalar<byte[]>.CreateError("Client data is destroyed or not inited!");
            }

            return OperationResultScalar<byte[]>.CreateSuccessful(_clientData);
        }

        public OperationResultScalar<string> GetClientDataBase64()
        {
            var result = GetClientData();
            if (result.IsSuccessful)
            {
                var error = IEUSignCP.BASE64Encode(result.Value, out var base64String);
                if (error == IEUSignCP.EU_ERROR_NONE)
                {
                    return OperationResultScalar<string>.CreateSuccessful(base64String);
                }
                return OperationResultScalar<string>.CreateEUSignError(error);
            }
            return OperationResultScalar<string>.CreateError(result);
        }

        #endregion

        #region Encrypt

        public OperationResultScalar<byte[]> EncryptObject<T>(T obj, JsonSerializerOptions jsonSerializerOptions = null)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(obj, jsonSerializerOptions);

                return EncryptBytes(bytes);
            }
            catch (Exception e)
            {
                return OperationResultScalar<byte[]>.CreateError(e);
            }
        }
        public OperationResultScalar<byte[]> EncryptBytes(byte[] bytes)
        {
            try
            {
                var sessionResult = GetSession();

                if (sessionResult.IsSuccessful == false)
                {
                    return OperationResultScalar<byte[]>.CreateError(sessionResult);
                }

                var session = sessionResult.Value;

                var error = IEUSignCP.SessionEncrypt(session, bytes, out var encryptedData);

                if (error == IEUSignCP.EU_ERROR_NONE)
                {
                    return OperationResultScalar<byte[]>.CreateSuccessful(encryptedData);
                }

                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }
            catch (Exception e)
            {
                return OperationResultScalar<byte[]>.CreateError(e);
            }
        }

        public OperationResultScalar<string> EncryptObjectToBase64<T>(T obj, JsonSerializerOptions jsonSerializerOptions = null)
        {
            try
            {
                var result = EncryptObject(obj, jsonSerializerOptions);

                if (result.IsSuccessful)
                {
                    var error = IEUSignCP.BASE64Encode(result.Value, out var base64String);

                    if (error == IEUSignCP.EU_ERROR_NONE)
                    {
                        return OperationResultScalar<string>.CreateSuccessful(base64String);
                    }

                    return OperationResultScalar<string>.CreateEUSignError(error);
                }

                return OperationResultScalar<string>.CreateError(result);
            }
            catch (Exception e)
            {
                return OperationResultScalar<string>.CreateError(e);
            }
        }
        public OperationResultScalar<string> EncryptBytesToBase64(byte[] bytes)
        {
            try
            {
                var result = EncryptBytes(bytes);

                if (result.IsSuccessful)
                {
                    var error = IEUSignCP.BASE64Encode(result.Value, out var base64String);

                    if (error == IEUSignCP.EU_ERROR_NONE)
                    {
                        return OperationResultScalar<string>.CreateSuccessful(base64String);
                    }

                    return OperationResultScalar<string>.CreateEUSignError(error);
                }

                return OperationResultScalar<string>.CreateError(result);
            }
            catch (Exception e)
            {
                return OperationResultScalar<string>.CreateError(e);
            }
        }

        #endregion

        #region Decrypt

        public OperationResultScalar<T> DecryptObject<T>(byte[] bytes, JsonSerializerOptions jsonSerializerOptions = null)
        {
            try
            {
                var decryptResult = DecryptBytes(bytes);

                if (decryptResult.IsSuccessful == false)
                {
                    return OperationResultScalar<T>.CreateError(decryptResult);
                }

                var obj = JsonSerializer.Deserialize<T>(decryptResult.Value, jsonSerializerOptions);

                return OperationResultScalar<T>.CreateSuccessful(obj);
            }
            catch (Exception e)
            {
                return OperationResultScalar<T>.CreateError(e);
            }
        }
        public OperationResultScalar<byte[]> DecryptBytes(byte[] bytes)
        {
            try
            {
                var sessionResult = GetSession();

                if (sessionResult.IsSuccessful == false)
                {
                    return OperationResultScalar<byte[]>.CreateError(sessionResult);
                }

                var session = sessionResult.Value;

                var error = IEUSignCP.SessionDecrypt(session, bytes, out var deryptedData);

                if (error == IEUSignCP.EU_ERROR_NONE)
                {
                    return OperationResultScalar<byte[]>.CreateSuccessful(deryptedData);
                }

                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }
            catch (Exception e)
            {
                return OperationResultScalar<byte[]>.CreateError(e);
            }
        }

        public OperationResultScalar<T> DecryptObjectFromBase64<T>(string base64String, JsonSerializerOptions jsonSerializerOptions = null)
        {
            try
            {
                var error = IEUSignCP.BASE64Decode(base64String, out var bytes);

                if (error == IEUSignCP.EU_ERROR_NONE)
                {
                    return DecryptObject<T>(bytes, jsonSerializerOptions);
                }

                return OperationResultScalar<T>.CreateEUSignError(error);
            }
            catch (Exception e)
            {
                return OperationResultScalar<T>.CreateError(e);
            }
        }
        public OperationResultScalar<byte[]> DecryptBytesFromBase64(string base64String)
        {
            try
            {
                var error = IEUSignCP.BASE64Decode(base64String, out var bytes);

                if (error == IEUSignCP.EU_ERROR_NONE)
                {
                    return DecryptBytes(bytes);
                }

                return OperationResultScalar<byte[]>.CreateEUSignError(error);
            }
            catch (Exception e)
            {
                return OperationResultScalar<byte[]>.CreateError(e);
            }
        }

        #endregion

        public void Destroy()
        {
            if (_session == default) return;

            IEUSignCP.SessionDestroy(_session);

            _session = default;
            _clientData = null;
            ValidTo = DateTime.UtcNow;
        }

        public static OperationResultScalar<EUSignCPSession> Create(byte[] certificate)
        {
            if (!IEUSignCP.IsInitialized())
            {
                var initError = IEUSignCP.Initialize();
                if (initError != IEUSignCP.EU_ERROR_NONE)
                {
                    return OperationResultScalar<EUSignCPSession>.CreateEUSignError(initError);
                }
            }

            var validTo = DateTime.UtcNow.AddSeconds(SessionExpireTimeInSeconds);
            var error = IEUSignCP.ClientDynamicKeySessionCreate(SessionExpireTimeInSeconds + SessionExpireTimeInSecondsDelta, certificate, out var sessionPtr, out var clientData);

            if (error == IEUSignCP.EU_ERROR_NONE)
            {
                var session = new EUSignCPSession(sessionPtr, clientData, validTo);
                return OperationResultScalar<EUSignCPSession>.CreateSuccessful(session);
            }

            return OperationResultScalar<EUSignCPSession>.CreateEUSignError(error);
        }
    }
}
