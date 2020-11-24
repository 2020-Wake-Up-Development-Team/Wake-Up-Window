using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WakeUp
{
    public class NetworkManager
    {
        private static RestRequest AddToRequest(RestRequest restRequest, string json = null, UrlSegment[] urlSegments = null, Header[] headers = null)
        {
            if (urlSegments != null)
            {
                foreach (var urlSegment in urlSegments)
                {
                    restRequest.AddUrlSegment(urlSegment.Name, urlSegment.Value);
                }
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    restRequest.AddHeader(header.Name, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(json))
            {
                restRequest.AddHeader("Content-Type", "application/json");
                restRequest.AddParameter("application/json", json, ParameterType.RequestBody);
            }

            return restRequest;
        }

        private static RestClient CreateClient(string resource)
        {
            var restClient = new RestClient(resource) { Timeout = 30000 };
            return restClient;
        }

        private static RestRequest CreateRequest(string resource, Method method, string parameterJson, UrlSegment[] urlSegments = null, Header[] headers = null)
        {
            var restRequest = new RestRequest(resource, method) { Timeout = 30000 };
            restRequest = AddToRequest(restRequest, parameterJson, urlSegments, headers);

            return restRequest;
        }

        private static T DeserializeSnakeCase<T>(string json)
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            try
            {
                var resp = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    ContractResolver = contractResolver,
                    Formatting = Formatting.Indented,
                });
                return resp;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return default(T);
        }

        public async Task<T> GetResponse<T>(string resource, Method method, string parameterJson = null, UrlSegment[] urlSegments = null, Header[] headers = null)
        {
            var client = CreateClient(resource);
            var restRequest = CreateRequest(resource, method, parameterJson, urlSegments, headers);
            var response = await client.ExecuteAsync(restRequest);

            var resp = DeserializeSnakeCase<T>(response.Content);

            return resp;
        }
    }

    public class UrlSegment
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public UrlSegment(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Header(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class Response<T>
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        public T Data { get; set; }

        public static implicit operator Response<T>(Response<string> v)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Nothing
    {
        public static Nothing AtAll => null;
    }
}
