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
open FSharp.Json
open Twitter.DataTypes
open Twitter.DataTypes.Request
open Twitter.DataTypes.Response
open Twitter.SQLQueries
open Akka.FSharp

open FSharp.Json






let connectionStringMemory = sprintf "Data Source=:memory:;Version=3;New=True;" 
//let connection = new SQLiteConnection(connectionStringMemory)
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
            let tweetId = tweet.uid + timestamp
            connection.Open()
            SQLQueries.dbInsertTweet tweetId tweet.tweet tweet.uid tweet.isRetweet tweet.origOwner connection
            
            //if not a retweet we need to put mentions and hash tags
            if not tweet.isRetweet then
                for hashtag in tweet.hashtags do
                    SQLQueries.dbInsertHashTag hashtag connection
                for mention in tweet.mentions do
                    SQLQueries.dbInsertMention tweetId tweet.tweet mention tweet.uid connection
                
            connection.Close()
 
    
    
        return! loop()    
    }
    
    loop()


    



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
            printf "tweet"
            
        | "retweet" ->
            printf "retweet"
            
        | "follow" ->
            printf "follow"
        | "feed" ->
            printf "feed"
        
        | "search" ->
            printf "search" // can be of multiple types
        
        return! loop()
    }
    loop()











//let's create user table


