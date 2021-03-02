using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace twitter {
    public class Program {
        private static TwitterClient TwitterClient;
        public static List<Hashtag> Hashtags { get; private set; } =
            new List<Hashtag>();
        private static string TwitterKey { get; set; }
        private static string TwitterSecret { get; set; }
        public static string ConnectionString { get; private set; }
        private static int StreamLimit;

        static void Main(string[] args) {
            Console.WriteLine("Starting program");
            LoadConfig();
            TwitterClient = new TwitterClient(
                TwitterKey, TwitterSecret, StreamLimit
            )
            { BaseAddress = new Uri("https://api.twitter.com") };
            new StreamFilter(TwitterClient, string.Format(
                    "({0}) has:hashtags",
                    string.Join(" OR ", Emoji.Happy_Emoji)
                ), "tweets with hashtags and emoji");
            WaitForDbStartup();
            _ = ViewResultsPeriodically();
            while (!TwitterClient.Complete) { TwitterClient.EnableStream(); }
            Console.WriteLine("Exiting program: TwitterClient reached StreamLimit.");
        }

        public static void LoadConfig() {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true).Build();
            TwitterKey = configuration.GetValue<string>(
                "AppSettings:twitterKey"
            );
            TwitterSecret = configuration.GetValue<string>(
                "AppSettings:twitterSecret"
            );
            ConnectionString = configuration.GetValue<string>(
                "ConnectionStrings:postgres"
            );
            StreamLimit = configuration.GetValue<int>(
                "AppSettings:StreamLimit"
            );
        }

        public static void WaitForDbStartup() {
            var databaseDown = true;
            while (databaseDown) {
                try {
                    Postgres.TryDbUp();
                    databaseDown = false;
                } catch {
                    Console.WriteLine("Database not responding, retrying in 5s...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        public static void AddOrUpdateHashtag(
            Match hashtag,
            DateTime dateHour
        ) {
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
                    var table = new List<string>(){
                        $"{new string('_', 54)}",
                        $"|Hourly popular hashtag review{new string(' ', 23)}|",
                        $"|{new string('_', 52)}|",
                        String.Format(
                            "|{0,-40}||{1,10}|", "Hashtag", "Happiness"
                        ),
                        $"|{new string('=', 52)}|",
                    };
                    Postgres.GetRecentPopularHashtags().ForEach(
                        (Hashtag h) => table.Add(String.Format(
                            "|{0,-40}||{1,10}|", h.name.Trim(), h.happiness
                        ))
                    );
                    table.Add($"|{new string('_', 52)}|");
                    Console.WriteLine(String.Join(Environment.NewLine, table));

                    var now = DateTime.UtcNow;
                    var previousTrigger = new DateTime(
                        now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Kind
                    );
                    var nextTrigger = previousTrigger + TimeSpan.FromHours(1);
                    Thread.Sleep(nextTrigger - now);
                });
            }
        }
    }
}
