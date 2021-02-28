using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace twitter {
    public class StreamFilter {
        private List<StreamRule> add { get; }
        private class StreamRule {
            public string value { get; set; }
            public string tag { get; set; }
        }
        public StreamFilter(HttpClient client, string value, string tag) {
            add = new List<StreamRule>{
            new StreamRule(){
                value = value,
                tag = tag,
            }
        };

            client.PostAsync(
                new Uri("https://api.twitter.com/2/tweets/search/stream/rules"),
                new StringContent(
                    JsonSerializer.Serialize(this),
                    System.Text.Encoding.UTF8,
                    "application/json"
                )
            );

            var streamRules = client.GetStringAsync(
                new Uri(
                    "https://api.twitter.com/2/tweets/search/stream/rules"
                )
            ).Result;

            Debug.WriteLine($"Implemented StreamRules: {streamRules} ");
        }
    }
}
