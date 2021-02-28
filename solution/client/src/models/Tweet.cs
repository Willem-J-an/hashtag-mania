using System;
using System.Text.RegularExpressions;

namespace twitter {
    public class Tweet {
        public TweetData data { get; set; }
        public MatchCollection hashtags { get; set; }
        public DateTime dateHour { get; set; }
        public class TweetData {
            public string id { get; set; }
            public string text { get; set; }
        }

        public Tweet ExtractHashtags() {
            this.hashtags = Regex.Matches(this.data.text, @"\B(\#[a-zA-Z]+\b)(?!;)");
            // Despite filtering with has:hashtags, some tweets come
            // through without hashtags, seems like it happens when
            // they link to tweets that do have hashtags. Tweets with
            // no hashtags are filtered out.
            var now = DateTime.UtcNow;
            this.dateHour = new DateTime(
                now.Year, now.Month, now.Day, now.Hour, 0, 0
            );
            return this;
        }
    }
}
