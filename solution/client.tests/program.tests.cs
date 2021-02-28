using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using twitter;

namespace client.tests
{
    public class Program_Tests
    {
        public static Tweet Setup_Program_Hashtags(string test)
        {
            var tweet = new Tweet()
            {
                data = new Tweet.TweetData
                {
                    id = "123",
                    text = String.Format(
                        "{0} #TESTDEVHashtags{1} #TESTDEVTrending{1}",
                        "This is a tweet with", test
                    )
                }
            }.ExtractHashtags();
            foreach (Match hashtag in tweet.hashtags)
            {
                Program.AddOrUpdateHashtag(hashtag, tweet.dateHour);
            }
            return tweet;
        }

        [Test]
        public void AddOrUpdateHashtag_Updates_hashtag()
        {
            var tweets = new List<Tweet>(){
                Setup_Program_Hashtags("update"),
                Setup_Program_Hashtags("update")
            };
            foreach (Match hashtag in tweets[0].hashtags)
            {
                var hashtagSaved = Program.Hashtags.Find(
                    (Hashtag hashtagSaved) =>
                    {
                        return hashtagSaved.name == hashtag.ToString() &&
                        hashtagSaved.datehour == tweets[0].dateHour;
                    }
                );
                Assert.AreEqual(2, hashtagSaved.happiness);
            }
        }

        [Test]
        public void AddOrUpdateHashtag_Adds_hashtag()
        {
            Tweet tweet = Setup_Program_Hashtags("add");
            foreach (Match hashtag in tweet.hashtags)
            {
                var hashtagSaved = Program.Hashtags.Find(
                    (Hashtag hashtagSaved) =>
                    {
                        return hashtagSaved.name == hashtag.ToString() &&
                        hashtagSaved.datehour == tweet.dateHour;
                    }
                );
                Assert.NotNull(hashtagSaved);
            }
        }

        [Test]
        public void ClearPersistedHashtag_Removes_Hashtags_From_List()
        {
            Tweet tweet = Setup_Program_Hashtags("clear");
            int hashtagCount = Program.Hashtags.Count;
            Program.Hashtags[0].persisted = true;
            Program.ClearPersistedHashtags();
            Assert.AreEqual(hashtagCount - 1, Program.Hashtags.Count);
        }
    }
}
[SetUpFixture]
public class TearDown
{
    [OneTimeTearDown]
    public void ClearTestHashtags()
    {
        Console.WriteLine("Clearing database testdata...");
        Program.LoadConfig();
        try
        {
            if(Postgres.TryDbUp()){
                Postgres.RemoveHashtagByPrefix("#TESTDEV");
            }
        }
        catch { 
            Console.WriteLine("Database not up, no testdata will be cleared.");
        }
    }
}
