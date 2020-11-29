module Twitter.JsonIO
open FSharp.Data

open Newtonsoft.Json


//Follow
type follow = JsonProvider<"""{
  "my_uid": 1,
  "to_uid": 2
}""">

//Retweet
type retweet = JsonProvider<"""{
    "my_uid" : 1,
    "tweet_id" : 2
}""">

//Tweet
type tweet = JsonProvider<"""{
  "uid": 12324,
  "tweetId": 233,
  "mentions": [
    1234,
    455
  ],
  "hashtags": [
    "lorem",
    "ipsum"
  ]
}""">


//Login
type login = JsonProvider<"""{
  "uid":1
}""">
