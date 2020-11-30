module Twitter.Server

//server will have a receiver actor first, which will recieve all the messages and take decisions
//every function will have it's own actor
//so actors will be

//Register Account actor-> Registers account and sends back OK message.
//LoginUser -> user logs in, a random number is shared as repsonse ignore number. Log the metric
//LogOutUser-> not sure how it will be used right now, but will log the metric
//MentionsInsert
//HashTagInsert
//GetFeed -> will show mentions, tweets I follow
//GetMyTweet -> will show my tweets-- Will filter mentions too. not sure if actually needed but lets put it
//Search -> Interesting one-> filter based on hashtag or my mentions 


//


//

open System.Data.SQLite
type User =
    {
        UID : string
    }
    
type Tweet = {
    UID : int
    TweetId: string
    Tweet : string
}

type Followers = {
    UID: int
    Follows: int
}

type Mentions = {
    TweetId : string
    UID : int
}

type HashTag = {
    hashtagID : string
    hashtag : int
}
type HashTagTable = {
    tweetId : string
    hashtagId : string
}


let connectionStringMemory = sprintf "Data Source=:memory:;Version=3;New=True;" 
let connection = new SQLiteConnection(connectionStringMemory)


connection.Open()

//let's create user table


