namespace VchasnoCapConsole.Integration.ApiClient.Enums
{
    enum ApiClientHttpMethod
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE,
    }

    static class ApiClientHttpMethodUtil
    {
        internal static bool HasContent(this ApiClientHttpMethod method) =>
            method == ApiClientHttpMethod.POST ||
            method == ApiClientHttpMethod.PUT ||
            method == ApiClientHttpMethod.PATCH ||
            method == ApiClientHttpMethod.DELETE;
    }
}
