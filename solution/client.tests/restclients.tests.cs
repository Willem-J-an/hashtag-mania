using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using twitter;

namespace client.tests {
    public class TwitterClient_Tests {
        private IConfigurationSection AppSettings { get; set; }
        private const int StreamLimit = 10;
        private TwitterClient TwitterClient { get; set; }

        [SetUp]
        public void Setup() {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true).Build();
            AppSettings = configuration.GetSection("AppSettings");
        }

        [Test]
        public void Instantiation_Does_Not_Throw() {
            Assert.DoesNotThrow(
                () => {
                    this.TwitterClient = new TwitterClient(
                        AppSettings.GetSection("twitterKey").Value,
                        AppSettings.GetSection("twitterSecret").Value,
                        StreamLimit);
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
                AppSettings.GetSection("twitterKey").Value,
                AppSettings.GetSection("twitterSecret").Value,
                1
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
                AppSettings.GetSection("twitterKey").Value,
                AppSettings.GetSection("twitterSecret").Value,
                1
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
        private IConfigurationSection AppSettings { get; set; }
        private TwitterClient TwitterClient { get; set; }

        [SetUp]
        public void Setup() {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true).Build();
            AppSettings = configuration.GetSection("AppSettings");
            this.TwitterClient = new TwitterClient(
                AppSettings.GetSection("twitterKey").Value,
                AppSettings.GetSection("twitterSecret").Value,
                1
            );
        }
        [Test]
        public void StreamFilter_Does_Not_Throw() {
            Assert.DoesNotThrow(() =>
               new StreamFilter(TwitterClient, string.Format(
                   "({0}) has:hashtags",
                    string.Join(" OR ", Emoji.Happy_Emoji)
               ), "tweets with hashtags and emoji")
            );
        }
    }
}
