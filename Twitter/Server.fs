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

open System.Collections.Generic
open System.Data.SQLite
open Akka.Actor
open FSharp.Data.JsonProvider
open FSharp.Json
open Twitter.DataTypes
open Twitter.DataTypes.Request
open Twitter.DataTypes.Response
open Akka.FSharp







let connectionStringMemory = sprintf "Data Source=:memory:;Version=3;New=True;" 
let connection = new SQLiteConnection(connectionStringMemory)
//table creation logic 
connection.Open()
//create user table
let mutable command = new SQLiteCommand (SQLQueries.createUserTableQuery, connection)
command.ExecuteNonQuery |> ignore
//create follower table
command <- new SQLiteCommand (SQLQueries.createFollowerTable, connection)
command.ExecuteNonQuery |> ignore

//create tweet table
command <- new SQLiteCommand (SQLQueries.createTweetTable, connection)
command.ExecuteNonQuery |> ignore

//create mention table
command <- new SQLiteCommand (SQLQueries.createMentionTable, connection)
command.ExecuteNonQuery |> ignore

//create hashtag table
command <- new SQLiteCommand (SQLQueries.createHashTagTable, connection)
command.ExecuteNonQuery |> ignore

//create hashtagTweet table
command <- new SQLiteCommand (SQLQueries.createHashTagTweetTable, connection)
command.ExecuteNonQuery |> ignore

//create feed table
command <- new SQLiteCommand (SQLQueries.feedTable, connection)
command.ExecuteNonQuery |> ignore

connection.Close()
//connection.Open()
let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 0
            }
        }"
 
let system = System.create "twitterClient" config

let mutable liveActorMap = new Dictionary<string, IActorRef>()


let responseSend (actor : IActorRef) (option : string) (data : string) =
    let response : Response.masterData = {
        option = option
        data = data
    }
    actor <! Json.serialize response
 
let registerActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | "init" ->
            printf "init might use later"
        | _ ->
            let res = Json.deserialize<Request.registerRequest> message
            let connection = new SQLiteConnection(connectionStringMemory)
            SQLQueries.dbAddNewUser res.uid res.password connection
            printf "user added"
        return! loop()
    }
    loop()
    
let RegistrationActor = spawn system "REGISTER_ACTOR" registerActor

let getFeedActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | "init" ->
            printf "init felt cute, might remove later lol"
        | _ ->
            let res = Json.deserialize<Request.feedRequest> message
            let connection = new SQLiteConnection(connectionStringMemory)
            let feed  = SQLQueries.dbGetFeed res.uid connection
            responseSend mailBox.Context.Sender DataTypes.Response.types.feedResponse (Json.serialize feed)
            
            //preparing response
            
        
        return! loop()
    }
    loop()
    
let FeedActor = spawn system "FEED_ACTOR" getFeedActor
let updateFeedsActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | "init" ->
            printf "init"
        | _ ->
            let tweet = Json.deserialize<Request.tweetSubmitRequest> message
            let connection = new SQLiteConnection(connectionStringMemory)
            let followers = SQLQueries.dbGetUserFollowers tweet.uid connection
            connection.Open()
            for i in followers.rows do
                SQLQueries.dbInsertFeed i.userId tweet.tweetId tweet.tweet tweet.uid tweet.isRetweet tweet.origOwner connection
                if liveActorMap.ContainsKey i.userId then
                    responseSend (liveActorMap.Item(i.userId))  (DataTypes.Response.types.sendTweetInFeed)  (Json.serialize tweet)
            
            connection.Close()
            
        return! loop()
    }
    
    loop()
    
let UpdateFeedActor = spawn system "UPDATE_FEED_ACTOR" updateFeedsActor
    
let tweetActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | "init" ->
            printf "felt cute, might remove later lol"
        | _ ->
            
            //let's get tweet
            let tweet = Json.deserialize<Request.tweetSubmitRequest> message
            let connection = new SQLiteConnection(connectionStringMemory)    
            //check if it's a retweet
            let timestamp = System.DateTime.Now.ToString()
            let tweetIdGen = tweet.uid + timestamp
            connection.Open()
            SQLQueries.dbInsertTweet tweetIdGen tweet.tweet tweet.uid tweet.isRetweet tweet.origOwner connection
            
            //if not a retweet we need to put mentions and hash tags
            if not tweet.isRetweet then
                for hashtag in tweet.hashtags do
                    SQLQueries.dbInsertHashTag hashtag connection
                for mention in tweet.mentions do
                    SQLQueries.dbInsertMention tweetIdGen tweet.tweet mention tweet.uid connection
                
            connection.Close()
            
 
//********************************  call feed actor here
            let tweetForFeed : Request.tweetSubmitRequest = {
                tweet = tweet.tweet
                tweetId = tweetIdGen
                //flag = tweet.flag
                isRetweet = tweet.isRetweet
                mentions = tweet.mentions
                hashtags = tweet.hashtags
                uid = tweet.uid
                origOwner = tweet.origOwner
                
            }
            FeedActor <! Json.serialize tweetForFeed

    
        return! loop()    
    }
    
    loop()


 
let TweetActor = spawn system "TWEET_ACTOR" tweetActor
    

let followActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive ()
        let res = Json.deserialize<Request.followRequest> message
        let connection = new SQLiteConnection(connectionStringMemory)
        SQLQueries.dbInsertFollow res.uid res.follow_id connection
        
        printf "user followed"
        return! loop()
    }
    loop()
    
let FollowActor = spawn system "FOLLOW_ACTOR" followActor
//search actor have types
    //search hashtags
    //search tweets with mymentions
    //search all users
    //search tweet with hashtags
    
    
let searchActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        //second time parsing will be done here
        let! message  = mailBox.Receive ()
        
        let searchMaster = Json.deserialize<Request.searchMaster> message
        match searchMaster.option with
        | Request.myMentionSearch ->
            let data = Json.deserialize<Request.searchMyMentionRequest> searchMaster.data
            let connection = new SQLiteConnection(connectionStringMemory)

            let res = SQLQueries.dbGetMentionsOfUser data.uid connection
            responseSend mailBox.Context.Sender DataTypes.Response.types.mentionResponse (Json.serialize res)
           
            printf "mentionSearchCalled"
        
        | Request.allHashtagSearch ->
            let data = Json.deserialize<Request.searchAllHashTags> searchMaster.data
            let connection = new SQLiteConnection(connectionStringMemory)

            let res = SQLQueries.dbGetAllHashTag connection
            responseSend mailBox.Context.Sender DataTypes.Response.types.allHashTagSearchResponse (Json.serialize res)
            
            
        | Request.userSearch ->
            printf ""
            let data = Json.deserialize<Request.searchAllUsers> searchMaster.data
            
            let connection = new SQLiteConnection(connectionStringMemory)
            let res = SQLQueries.dbGetAllUsers connection
            responseSend mailBox.Context.Sender DataTypes.Response.types.allUserInfoResponse (Json.serialize res)
            
        | Request.tweetWithHashTagSearch ->
            let data = Json.deserialize<Request.searchTweetWithHashTagRequest> searchMaster.data
            let connection = new SQLiteConnection(connectionStringMemory)
            let res = SQLQueries.dbGetTweetWithTag data.hashtag connection
            responseSend mailBox.Context.Sender DataTypes.Response.types.hashTagTweetsResponse (Json.serialize res)
            
        | _ ->
            printf "someError"
        
        
        
        return! loop()
    }
    loop()
    
let SearchActor = spawn system "SEARCH_ACTOR" followActor

let serverActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive ()
        let res = Json.deserialize<Request.masterData> message
        match res.option with
        | Request.types.registerRequest ->
            //register user here
            printf "registering user here"
            
        | Request.types.loginRequest ->
            printf "login"
            let loginData = Json.deserialize<Request.loginRequest> res.data
            liveActorMap.Add(loginData.uid, mailBox.Context.Sender)
            
            
        | Request.types.logoutRequest ->
            printf "logout"
            let logoutData = Json.deserialize<Request.logoutRequest> res.data
            liveActorMap.Remove(logoutData.uid) |> ignore
            
        | Request.types.submitTweetRequest ->
            TweetActor <! res.data
            
        | Request.types.submitReTweetRequest ->
            TweetActor <! res.data
            
        | Request.types.followRequest ->
            printf "follow"
            FollowActor <! res.data
        | Request.types.feedRequest ->
            FeedActor <! res.data
        
        | Request.types.searchRequest ->
            printf "search" // can be of multiple types
            SearchActor <! res.data
        
        | _ ->
            printf "someError"
        
        return! loop()
    }
    loop()




let ServerActor = spawn system "server" followActor







//let's create user table


