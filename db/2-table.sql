CREATE TABLE IF NOT EXISTS twitter.happy_hashtags (
    name character(280),
    datehour timestamp(0),
    happiness integer,
    PRIMARY KEY (name, datehour)
);
ALTER TABLE twitter.happy_hashtags
    OWNER to postgres;