using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

class Program {
    private static TwitterClient TwitterClient;
    public static List<Hashtag> Hashtags { get; private set; } =
        new List<Hashtag>();
    private static IConfigurationSection AppSettings { get; set; }
    public static string ConnectionString { get; private set; }
    private const int StreamLimit = 500; // Set a limit or 0 for no limit

    static void Main(string[] args) {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true).Build();
        AppSettings = configuration.GetSection("AppSettings");
        ConnectionString = configuration.GetConnectionString("postgres");
        TwitterClient = new TwitterClient(
            AppSettings.GetSection("twitterKey").Value,
            AppSettings.GetSection("twitterSecret").Value,
            StreamLimit
        );
        new StreamFilter(TwitterClient, string.Format(
                "({0}) has:hashtags",
                string.Join(" OR ", Emoji.Happy_Emoji)
            ), "tweets with hashtags and emoji");

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
}
