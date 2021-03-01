using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Text.Json;
using twitter;

namespace client.tests {
    public class TwitterClient_Tests {
        private string TwitterKey { get; set; }
        private string TwitterSecret { get; set; }
        private TwitterClient TwitterClient { get; set; }

        [SetUp]
        public void Setup() {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true).Build();
            this.TwitterKey = configuration.GetValue<string>(
                "AppSettings:twitterKey"
            );
            this.TwitterSecret = configuration.GetValue<string>(
                "AppSettings:twitterSecret"
            );
        }

        [Test]
        public void Instantiation_Does_Not_Throw() {
            Assert.DoesNotThrow(
                () => {
                    this.TwitterClient = new TwitterClient(
                        this.TwitterKey, this.TwitterSecret, 10
                    );
                }
            );
        }

        [Test]
        public void Request_Does_Not_Throw() {
            Assert.DoesNotThrowAsync(
                () => this.TwitterClient.GetStringAsync(
                "https://api.twitter.com/2/tweets/search/recent?max_results=10&query=test"
                )
            );
        }

        [Test]
        public void BackoffHandler_RateLimit_Exception_Sets_Correct_Delay() {
            this.TwitterClient = new TwitterClient(
                this.TwitterKey, this.TwitterSecret, 10
            );
            this.TwitterClient.Stopwatch = new Stopwatch();
            this.TwitterClient.Stopwatch.Start();
            this.TwitterClient.BackoffHandler(true, true);
            Assert.AreEqual(this.TwitterClient.Delay, TimeSpan.FromMinutes(1));

            for (int i = 0; i < 4; i++) {
                this.TwitterClient.BackoffHandler(true, true);
            }
            Assert.AreEqual(this.TwitterClient.Delay, TimeSpan.FromMinutes(16));
        }

        [Test]
        public void BackoffHandler_Http_Exception_Sets_Correct_Delay() {
            this.TwitterClient = new TwitterClient(
                this.TwitterKey, this.TwitterSecret, 10
            );
            this.TwitterClient.Stopwatch = new Stopwatch();
            this.TwitterClient.Stopwatch.Start();
            this.TwitterClient.BackoffHandler(true, false);
            Assert.AreEqual(this.TwitterClient.Delay, TimeSpan.FromMinutes(0));
            this.TwitterClient.BackoffHandler(true, false);
            Assert.AreEqual(this.TwitterClient.Delay, TimeSpan.FromSeconds(10));
            for (int i = 0; i < 4; i++) {
                this.TwitterClient.BackoffHandler(true, false);
            }
            Assert.AreEqual(TimeSpan.FromSeconds(160), this.TwitterClient.Delay);
            Assert.Throws<System.InvalidOperationException>(
                () => this.TwitterClient.BackoffHandler(true, false)
            );
        }
    }

    public class StreamFilter_Tests {
        private string TwitterKey { get; set; }
        private string TwitterSecret { get; set; }
        private TwitterClient TwitterClient { get; set; }

        [SetUp]
        public void Setup() {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true).Build();
            this.TwitterKey = configuration.GetValue<string>(
                "AppSettings:twitterKey"
            );
            this.TwitterSecret = configuration.GetValue<string>(
                "AppSettings:twitterSecret"
            );
        }

        [Test]
        public void StreamFilter_Does_Not_Throw() {
            this.TwitterClient = new TwitterClient(
                this.TwitterKey, this.TwitterSecret, 10
            );
            Assert.DoesNotThrow(() =>
               new StreamFilter(this.TwitterClient, string.Format(
                   "({0}) has:hashtags",
                    string.Join(" OR ", Emoji.Happy_Emoji)
               ), "tweets with hashtags and emoji")
            );
        }
    }
}
