using API.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace API.Extensions;

public static class HttpExtensions
{
    private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver() 
    };

    public static void AddPaginationHeader(this HttpResponse response, PaginationHeader header)
    {
        response.Headers.Add("Pagination", JsonConvert.SerializeObject(header, _jsonSettings));
        response.Headers.Add("Access-Control-Expose-Headers", "Pagination");
    }
}