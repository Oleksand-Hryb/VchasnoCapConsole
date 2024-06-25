namespace VchasnoCapConsole.Integration.ApiClient.Enums
{
    public enum ApiClientHttpMethod
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE,
    }

    public static class ApiClientHttpMethodUtil
    {
        internal static bool HasContent(this ApiClientHttpMethod method) =>
            method == ApiClientHttpMethod.POST ||
            method == ApiClientHttpMethod.PUT ||
            method == ApiClientHttpMethod.PATCH ||
            method == ApiClientHttpMethod.DELETE;
    }
}
