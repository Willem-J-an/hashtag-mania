using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace twitter {
    public class TwitterClient : HttpClient {
        public Stopwatch Stopwatch { get; set; }
        private int TweetCounter = 0;
        private int StreamLimit { get; }
        public TimeSpan Delay { get; private set; }
        private bool Persisting = false;
        private int StreamingExponentialRetry = 0;
        public bool Complete { get; private set; }

        public TwitterClient(
            string twitterKey,
            string twitterSecret,
            int streamLimit
        ) {
            this.StreamLimit = streamLimit;
            this.DefaultRequestHeaders.Accept.Clear();
            this.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            this.DefaultRequestHeaders.Authorization = new TwitterContext(
                twitterKey, twitterSecret
            ).AuthenticationHeaderValue;
            this.Timeout = System.TimeSpan.FromSeconds(20);
        }

        public void EnableStream() {
            Thread.Sleep(this.Delay);
            try {
                this.Stopwatch = new Stopwatch();
                this.Stopwatch.Start();
                this.StreamTweets();
            } catch (AggregateException exception) {
                if (exception.InnerException is HttpRequestException) {
                    this.HttpErrorHandler(
                        (HttpRequestException)exception.InnerException
                    );
                } else {
                    Debug.WriteLine("InnerException type not recognized.");
                    this.BackoffHandler(false, false);
                }
            } catch (HttpRequestException exception) {
                Debug.Write(exception);
                this.HttpErrorHandler(exception);
            } catch (Exception exception) {
                Debug.Write(exception);
                Debug.WriteLine("exception type not recognized.");
                this.BackoffHandler(false, false);
            }
        }
        private void StreamTweets() {
            var stream = this.GetStreamAsync(
                "/2/tweets/search/stream"
            ).Result;
            using (var reader = new StreamReader(stream)) {
                while (
                    !reader.EndOfStream && (
                        this.TweetCounter < this.StreamLimit ||
                        this.StreamLimit == 0
                    )
                ) {
                    this.ProcessTweet(reader.ReadLine());
                    this.PeriodicallyPersist();
                    this.DecrementRetryIfExists();
                }
                this.Complete = this.TweetCounter >= this.StreamLimit &&
                    this.StreamLimit != 0;
            }
        }

        private void PeriodicallyPersist() {
            if (Program.Hashtags.Count > 50 && this.Persisting == false) {
                this.Persisting = true;
                Postgres.PersistHashtags(
                    Program.Hashtags, Stopwatch, TweetCounter
                ).ContinueWith((Task task) => this.Persisting = false);
            };
        }

        private void ProcessTweet(string rawTweet) {
            try {
                Tweet tweet = JsonSerializer.Deserialize<Tweet>(rawTweet)
                    .ExtractHashtags();
                foreach (Match hashtag in tweet.hashtags) {
                    Program.AddOrUpdateHashtag(hashtag, tweet.dateHour);
                }
                TweetCounter++;
            } catch {
                Debug.WriteLine("Failed to parse a line as tweet.");
            };
        }

        private void DecrementRetryIfExists() {
            if (this.StreamingExponentialRetry > 1) {
                this.StreamingExponentialRetry /= 2;
            } else if (this.StreamingExponentialRetry == 1) {
                this.StreamingExponentialRetry -= 1;
            };
        }

        private void HttpErrorHandler(HttpRequestException exception) {
            this.BackoffHandler(
                true,
                exception.StatusCode ==
                    System.Net.HttpStatusCode.TooManyRequests
            );
        }

        public void BackoffHandler(bool httpError, bool rateLimit) {
            this.Stopwatch.Stop();
            Console.WriteLine(String.Format(
                "Stream was interupted after {0} seconds...",
                this.Stopwatch.Elapsed.TotalSeconds.ToString("F0")
            ));
            StreamingExponentialRetry = StreamingExponentialRetry == 0 ?
                1 : StreamingExponentialRetry * 2;
            if (rateLimit) {
                this.Delay = TimeSpan.FromMinutes(
                    1 * this.StreamingExponentialRetry
                );
                Console.WriteLine(String.Format(
                    "Attempting to recover from ratelimit {1}: {0} minutes...",
                    "after delay of", this.Delay.ToString("mm")
                ));
            } else if (StreamingExponentialRetry == 1) {
                Debug.WriteLine("Attempting to recover from http error...");
            } else if (StreamingExponentialRetry >= 64) {
                throw new System.InvalidOperationException(
                    "Stream could not be recovered... Needs operator attention."
                );
            } else {
                this.Delay = TimeSpan.FromSeconds(
                    5 * this.StreamingExponentialRetry
                );
                Console.WriteLine(String.Format(
                    "Attempting to recover from error {0} {1} minutes...",
                    "after delay of", this.Delay.TotalSeconds.ToString("F0")
                ));
            }
        }
    }
}
