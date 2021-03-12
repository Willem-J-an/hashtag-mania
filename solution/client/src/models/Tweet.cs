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
            var now = DateTime.UtcNow;
            this.dateHour = new DateTime(
                now.Year, now.Month, now.Day, now.Hour, 0, 0
            );
            return this;
        }
    }
}
