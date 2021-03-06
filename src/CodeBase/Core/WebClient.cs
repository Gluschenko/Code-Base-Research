﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace CodeBase
{
    public class WebClient
    {
        private static HttpClient client;

        public static readonly Action<HttpStatusCode> HTTPError = (code) =>
        {
            MessageHelper.Error($"{(int)code}/{code}", "HTTP Error");
        };

        public static void InitClient()
        {
            if (client == null)
            {
                client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", Hardware.GetID());
            }
        }

        public static async void Request(string URL, Action<HttpResponseMessage> response)
        {
            InitClient();

            try
            {
                var res = await client.GetAsync(URL);
                response(res);
            }
            catch (Exception ex)
            {
                MessageHelper.ThrowException(ex);
            }
        }

        public static async void PostRequest(string URL, Dictionary<string, string> fields, Action<HttpResponseMessage> response)
        {
            InitClient();

            using var message = new HttpRequestMessage(HttpMethod.Post, URL);
            var pairs = fields.Select(pair => $"{pair.Key}={pair.Value}");
            var query = string.Join("&", pairs);
            message.Content = new StringContent(query, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            try
            {
                var res = await client.SendAsync(message);
                response(res);
            }
            catch (Exception ex)
            {
                MessageHelper.ThrowException(ex);
            }
        }

        public static void ProcessResponse(HttpWebResponse response, Action<string> onSuccess, Action<HttpStatusCode> onFailure = null)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            onFailure ??= HTTPError;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                using StreamReader reader = new StreamReader(stream);
                string data = reader.ReadToEnd();
                onSuccess?.Invoke(data);
            }
            else
            {
                onFailure?.Invoke(response.StatusCode);
            }
        }

        public static async void ProcessResponse(HttpResponseMessage response, Action<string> onSuccess, Action<HttpStatusCode> onFailure = null)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            onFailure ??= HTTPError;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string data = await response.Content.ReadAsStringAsync();
                onSuccess?.Invoke(data);
            }
            else
            {
                onFailure?.Invoke(response.StatusCode);
            }
        }
    }

}