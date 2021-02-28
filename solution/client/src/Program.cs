using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace twitter {
    public class Program {
        private static TwitterClient TwitterClient;
        public static List<Hashtag> Hashtags { get; private set; } =
            new List<Hashtag>();
        private static string TwitterKey { get; set; }
        private static string TwitterSecret { get; set; }
        public static string ConnectionString { get; private set; }
        private const int StreamLimit = 500; // Set a limit or 0 for no limit

        static void Main(string[] args) {
            LoadConfig();
            TwitterClient = new TwitterClient(
                TwitterKey, TwitterSecret, StreamLimit
            );
            new StreamFilter(TwitterClient, string.Format(
                    "({0}) has:hashtags",
                    string.Join(" OR ", Emoji.Happy_Emoji)
                ), "tweets with hashtags and emoji");
            WaitForDbStartup();
            _ = ViewResultsPeriodically();
            while (!TwitterClient.Complete) {
                try {
                    TwitterClient.StreamTweets();
                } catch (AggregateException exception) {
                    if (exception.InnerException is HttpRequestException) {
                        TwitterClient.HttpErrorHandler(
                            (HttpRequestException)exception.InnerException
                        );
                    } else {
                        Debug.WriteLine("InnerException type not recognized.");
                        TwitterClient.BackoffHandler(false, false);
                    }
                } catch (HttpRequestException exception) {
                    Debug.Write(exception);
                    TwitterClient.HttpErrorHandler(exception);
                } catch (Exception exception) {
                    Debug.Write(exception);
                    Debug.WriteLine("exception type not recognized.");
                    TwitterClient.BackoffHandler(false, false);
                }
            }
            Debug.WriteLine("Exiting program: TwitterClient reached StreamLimit.");
        }

        public static void LoadConfig() {
            if (File.Exists("appsettings.json")) {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false, true).Build();
                var appSettings = configuration.GetSection("AppSettings");
                TwitterKey = appSettings.GetSection("twitterKey").Value;
                TwitterSecret = appSettings.GetSection("twitterSecret").Value;
                ConnectionString = configuration.GetConnectionString("postgres");
            } else {
                TwitterKey = System.Environment.GetEnvironmentVariable("twitterKey");
                TwitterSecret = System.Environment.GetEnvironmentVariable("twitterSecret");
                ConnectionString = System.Environment.GetEnvironmentVariable("postgres");
            }
            Console.WriteLine($"using {ConnectionString}");
        }

        public static void WaitForDbStartup() {
            var databaseDown = true;
            while (databaseDown) {
                try {
                    Postgres.TryDbUp();
                } catch {
                    Console.WriteLine("Database not responding, retrying in 5s...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        public static void AddOrUpdateHashtag(Match hashtag, DateTime dateHour) {
            var hashtagSaved = Hashtags.Find((Hashtag hashtagSaved) => {
                return hashtagSaved.name == hashtag.ToString() &&
                hashtagSaved.datehour == dateHour;
            });

            if (hashtagSaved is null) {
                Hashtags.Add(new Hashtag(hashtag.ToString(), dateHour));
            } else { hashtagSaved.happiness += 1; }
        }

        public static void ClearPersistedHashtags() {
            int countBefore = Hashtags.Count;
            Hashtags.RemoveAll((Hashtag hashtag) => {
                return hashtag.persisted;
            });

            Debug.WriteLine(String.Format(
                "Persist operation complete; {0}: {1}; {2}: {3}",
                "Current Tags", Hashtags.Count,
                "Persisted Tags to Database",
                countBefore - Hashtags.Count
            ));
        }

        private static async Task ViewResultsPeriodically() {
            while (true) {
                await Task.Run(() => {
                    Console.WriteLine("Hourly popular hashtag review:");
                    Postgres.GetRecentPopularHashtags().ForEach(
                        (Hashtag h) => {
                            Console.WriteLine(String.Format(
                                ">>> Hashtag: {0}; || Happiness: {1}",
                                h.name.Trim(), h.happiness
                            ));
                        }
                    );
                    var now = DateTime.UtcNow;
                    var previousTrigger = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Kind);
                    var nextTrigger = previousTrigger + TimeSpan.FromHours(1);
                    Thread.Sleep(nextTrigger - now);
                });
            }
        }
    }
}
