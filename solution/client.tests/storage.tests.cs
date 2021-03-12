using NUnit.Framework;
using System.Diagnostics;
using System.Threading.Tasks;
using twitter;

namespace client.tests {
    public class StorageClient_Tests {

        [Test]
        public async Task Postgres_PersistHashtags_Stores_To_Table() {
            Program.LoadConfig();
            Program_Tests.Setup_Program_Hashtags("persist");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await Postgres.PersistHashtags(
                Program.Hashtags, stopwatch, 1
            );
            stopwatch.Stop();
            Program.Hashtags.ForEach((Hashtag h) => {
                Hashtag hashtag = Postgres.GetHashtag(h);
                Assert.NotNull(hashtag);
            });
        }
    }
}
