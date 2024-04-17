using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json.Linq;

namespace golf1052.ThreadsAPI
{
    public class ThreadsClient
    {
        private const string BaseUrl = "https://graph.instagram.com/v19.0/";

        private string ClientId { get; set; }
        private string ClientSecret { get; set; }
        private string UserId { get; set; }
        private string? LongLivedAccessToken { get; set; }

        private readonly HttpClient httpClient;

        public ThreadsClient(string clientId, string clientSecret) : this(clientId, clientSecret, new HttpClient())
        {
        }

        public ThreadsClient(string clientId, string clientSecret, HttpClient httpClient)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            this.httpClient = httpClient;
        }

        public string GetAuthorizationUrl(string redirectUri, string scopes)
        {
            return "https://api.instagram.com/oauth/authorize" +
                $"?client_id={ClientId}" +
                $"&redirect_uri={redirectUri}" +
                $"&scope={scopes}" +
                "&response_type=code";
        }

        public async Task<string> GetShortLivedAccessToken(string redirectUri, string code)
        {
            Url url = new Url("https://api.instagram.com/oauth/access_token");
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
            string url = "https://graph.instagram.com/access_token" +
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
            string url = "https://graph.instagram.com/refresh_access_token" +
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

        public async Task PublishThreadsMediaContainer(string creationId)
        {
            Url url = new Url(BaseUrl).AppendPathSegments(UserId, "threads_publish")
                .SetQueryParam("access_token", LongLivedAccessToken)
                .SetQueryParam("creation_id", creationId);

            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            string responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new ThreadsException(responseString);
            }
        }

        public async Task<JObject> GetThreadsMediaObject(string mediaId,
            string fields)
        {
            Url url = new Url(BaseUrl).AppendPathSegment(mediaId)
                .SetQueryParam("fields", fields);
            HttpResponseMessage response = await httpClient.GetAsync(url);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JObject.Parse(responseString);
            }
            else
            {
                throw new ThreadsException(responseString);
            }
        }
    }
}
