using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace twitter {
    [Table("happy_hashtags", Schema = "twitter")]
    public class Hashtag {
        [Column(TypeName = "varchar(280)")]
        public string name { get; set; }
        public DateTime datehour { get; set; }
        public int happiness { get; set; }
        [NotMapped]
        public bool persisted = false;

        public Hashtag(
            string name,
            DateTime datehour,
            int happiness
        ) {
            this.name = name;
            this.datehour = datehour;
            this.happiness = happiness;
        }

        public Hashtag(string name, DateTime dateHour) {
            this.name = name;
            this.datehour = dateHour;
            this.happiness = 1;
        }
    }
}
