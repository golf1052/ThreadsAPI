using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using golf1052.ThreadsAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace golf1052.ThreadsAPI
{
    public class ThreadsClient
    {
        private const string BaseUrl = "https://graph.threads.net/v1.0/";

        public string? LongLivedAccessToken { get; set; }
        public string? UserId { get; set; }

        private string ClientId { get; set; }
        private string ClientSecret { get; set; }

        private readonly HttpClient httpClient;
        private readonly JsonSerializerSettings serializer;

        public ThreadsClient(string clientId, string clientSecret) : this(clientId, clientSecret, new HttpClient())
        {
        }

        public ThreadsClient(string clientId, string clientSecret, HttpClient httpClient)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            this.httpClient = httpClient;
            serializer = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
        }

        public string GetAuthorizationUrl(string redirectUri, string scopes)
        {
            return "https://threads.net/oauth/authorize" +
                $"?client_id={ClientId}" +
                $"&redirect_uri={redirectUri}" +
                $"&scope={scopes}" +
                "&response_type=code";
        }

        public async Task<string> GetShortLivedAccessToken(string redirectUri, string code)
        {
            Url url = new Url("https://graph.threads.net/oauth/access_token");
            List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(parameters);
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Content = content;
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject responseObject = JObject.Parse(responseString);
                return (string)responseObject["access_token"]!;
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }

        public async Task<string> GetLongLivedAccessToken(string shortLivedAccessToken)
        {
            string url = "https://graph.threads.net/access_token" +
                $"?grant_type=ig_exchange_token" +
                $"&client_secret={ClientSecret}" +
                $"&access_token={shortLivedAccessToken}";
            HttpResponseMessage response = await httpClient.GetAsync(url);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject responseObject = JObject.Parse(responseString);
                UserId = (string)responseObject["user_id"]!;
                return (string)responseObject["access_token"]!;
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }

        public async Task<string> RefreshLongLivedAccessToken(string longLivedAccessToken)
        {
            string url = "https://graph.threads.net/refresh_access_token" +
                $"?grant_type=ig_refresh_token" +
                $"&access_token={longLivedAccessToken}";
            HttpResponseMessage response = await httpClient.GetAsync(url);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject responseObject = JObject.Parse(responseString);
                LongLivedAccessToken = (string)responseObject["access_token"]!;
                return LongLivedAccessToken;
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }

        public async Task<string> CreateThreadsMediaContainer(string mediaType,
            string? text = null,
            string? imageUrl = null,
            string? videoUrl = null,
            string? replyToId = null,
            bool? isCarouselItem = null,
            List<string>? children = null)
        {
            Url url = new Url(BaseUrl).AppendPathSegments(UserId, "threads")
                .SetQueryParam("access_token", LongLivedAccessToken)
                .SetQueryParam("media_type", mediaType);

            if (text != null)
            {
                url.SetQueryParam("text", text);
            }

            if (imageUrl != null)
            {
                url.SetQueryParam("image_url", imageUrl);
            }

            if (videoUrl != null)
            {
                url.SetQueryParam("video_url", videoUrl);
            }

            if (replyToId != null)
            {
                url.SetQueryParam("reply_to_id", replyToId);
            }

            if (isCarouselItem != null)
            {
                url.SetQueryParam("is_carousel_item", isCarouselItem);
            }

            if (children != null)
            {
                url.SetQueryParam("children", $"{string.Join(',', children)}");
            }

            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject responseObject = JObject.Parse(responseString);
                return (string)responseObject["id"]!;
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }

        public async Task<string> PublishThreadsMediaContainer(string creationId)
        {
            Url url = new Url(BaseUrl).AppendPathSegments(UserId, "threads_publish")
                .SetQueryParam("access_token", LongLivedAccessToken)
                .SetQueryParam("creation_id", creationId);

            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject responseObject = JObject.Parse(responseString);
                return (string)responseObject["id"]!;
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }

        public async Task<ThreadsMediaObject> GetThreadsMediaObject(string mediaId,
            string fields)
        {
            Url url = new Url(BaseUrl).AppendPathSegment(mediaId)
                .SetQueryParam("fields", fields)
                .SetQueryParam("access_token", LongLivedAccessToken);
            HttpResponseMessage response = await httpClient.GetAsync(url);
            return await Deserialize<ThreadsMediaObject>(response);
        }

        private async Task<T> Deserialize<T>(HttpResponseMessage responseMessage)
        {
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            //System.Diagnostics.Debug.WriteLine(responseString);
            if (responseMessage.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(responseString, serializer)!;
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }
    }
}
