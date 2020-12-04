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
open System.Threading
open Akka.Actor
open FSharp.Data.JsonProvider
open FSharp.Json
open Twitter.DataTypes
open Twitter.DataTypes.Request
open Twitter.DataTypes.Response
open Akka.FSharp
open Twitter.Client
open Twitter.DataTypes.simulator


//let serverSetup = 

let databaseFilename = "/Users/dhananjaysarsonia/RiderProjects/Twitter/Twitter/tweeeeterdb"
SQLiteConnection.CreateFile(databaseFilename) 

//let connectionString =  sprintf "Data Source=%s;Version=3;" "sqliteFile.sqlite"
//let connectionStringMemory = sprintf "Data Source=:memory:;Version=3;cache=shared;"
let connectionStringMemory =  sprintf "Data Source=%s;Version=3;" databaseFilename
//let connectionStringMemory = connectionString 
let connection = new SQLiteConnection(connectionStringMemory)
//table creation logic 
connection.Open()

let command = new SQLiteCommand (SQLQueries.createUserTableQuery, connection)
command.ExecuteNonQuery() |> ignore
//create follower table
let command2 = new SQLiteCommand (SQLQueries.createFollowerTable, connection)
command2.ExecuteNonQuery() |> ignore

//create tweet table
let command3 = new SQLiteCommand (SQLQueries.createTweetTable, connection)
command3.ExecuteNonQuery() |> ignore

//create mention table
let command4 = new SQLiteCommand (SQLQueries.createMentionTable, connection)
command4.ExecuteNonQuery() |> ignore

//create hashtag table
let command5 = new SQLiteCommand (SQLQueries.createHashTagTable, connection)
command5.ExecuteNonQuery() |> ignore

//create hashtagTweet table
let command6 = new SQLiteCommand (SQLQueries.createHashTagTweetTable, connection)
command6.ExecuteNonQuery() |> ignore

//create feed table
let command7 = new SQLiteCommand (SQLQueries.feedTable, connection)
command7.ExecuteNonQuery() |> ignore

connection.Close()

//connection.Open()
let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 9001
            }
        }"
 
let serverSystem = System.create "serverSystem" config


type ServerDataWrapper = 
    |Request of string*IActorRef
    |Init 


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
        |Init ->
            printf "felt cute, might delete later"
      
        | ServerDataWrapper.Request (data, actorRef) ->
            let res = Json.deserialize<Request.registerRequest> data
            let connection = new SQLiteConnection(connectionStringMemory)
            SQLQueries.dbAddNewUser res.uid res.password connection
            responseSend actorRef DataTypes.Response.types.registerResponse "Registered"
            
        
        return! loop()
    }
    loop()
    
let RegistrationActor = spawn serverSystem "REGISTER_ACTOR" registerActor

let getFeedActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | Init ->
            printf "init felt cute, might remove later lol"
        | ServerDataWrapper.Request (data, actorRef) ->
            let res = Json.deserialize<Request.feedRequest> data
            let connection = new SQLiteConnection(connectionStringMemory)
            let feed  = SQLQueries.dbGetFeed res.uid connection
            responseSend actorRef DataTypes.Response.types.feedResponse (Json.serialize feed)
            
            //preparing response
            
        
        return! loop()
    }
    loop()
    
let FeedActor = spawn serverSystem "FEED_ACTOR" getFeedActor
let updateFeedsActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | Init ->
            printf "init"
        | ServerDataWrapper.Request (data, actorRef) ->
            let tweet = Json.deserialize<Request.tweetSubmitRequest> data
            let connection = new SQLiteConnection(connectionStringMemory)
            let followers = SQLQueries.dbGetUserFollowers tweet.uid connection
            connection.Open()
            for i in followers.rows do
                SQLQueries.dbInsertFeed i.userId tweet.tweetId tweet.tweet tweet.uid tweet.isRetweet tweet.origOwner connection
                if liveActorMap.ContainsKey i.userId then
                    responseSend (liveActorMap.Item(i.userId))  (DataTypes.Response.types.sendTweetInFeed)  (Json.serialize tweet)
            
            connection.Close()
            
            responseSend actorRef DataTypes.Response.types.sendTweetResponse "OK"
            
        return! loop()
    }
    
    loop()
    
let UpdateFeedActor = spawn serverSystem "UPDATE_FEED_ACTOR" updateFeedsActor
    
let tweetActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | Init ->
            printf "felt cute, might remove later lol"
        | ServerDataWrapper.Request (data, actorRef) ->
            
            //let's get tweet
            let tweet = Json.deserialize<Request.tweetSubmitRequest> data
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
            
            FeedActor <! ServerDataWrapper.Request((Json.serialize tweetForFeed), actorRef)
            

    
        return! loop()    
    }
    
    loop()


 
let TweetActor = spawn serverSystem "TWEET_ACTOR" tweetActor
    

let followActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive ()
        match message with
        | Init ->
            printf "Felt Cute, might remove later"
        | ServerDataWrapper.Request(data, actorRef) ->
            let res = Json.deserialize<Request.followRequest> data
            let connection = new SQLiteConnection(connectionStringMemory)
            SQLQueries.dbInsertFollow res.uid res.follow_id connection 
            responseSend actorRef DataTypes.Response.types.followResponse "OK"
            
        return! loop()
    }
    loop()
    
let FollowActor = spawn serverSystem "FOLLOW_ACTOR" followActor

    
    

let followMassActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive ()
        match message with
        | Init ->
            printf "Felt Cute, might remove later"
        | ServerDataWrapper.Request(data, actorRef) ->
            let res = Json.deserialize<DataTypes.simulator.followBulkData> data
            let connection = new SQLiteConnection(connectionStringMemory)
            connection.Open()
            for rowData in res.followList do
                SQLQueries.dbInsertFollow res.uid (string <| rowData) connection
            
            
            connection.Close()
            responseSend actorRef DataTypes.Response.types.followResponse "OK"
            
        return! loop()
    }
    loop()
    
let FollowMassActor = spawn serverSystem "FOLLOW_MASS_ACTOR" followMassActor
    
//search actor have types
    //search hashtags
    //search tweets with mymentions
    //search all users
    //search tweet with hashtags
    
    
let searchActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        //second time parsing will be done here
        let! message  = mailBox.Receive ()
        
        match message with
        | Init ->
            printf "init"
        | ServerDataWrapper.Request (data, actorRef) ->
            let searchMaster = Json.deserialize<Request.searchMaster> data
            match searchMaster.option with
            | Request.myMentionSearch ->
                let data = Json.deserialize<Request.searchMyMentionRequest> searchMaster.data
                let connection = new SQLiteConnection(connectionStringMemory)
                let res = SQLQueries.dbGetMentionsOfUser data.uid connection
                responseSend actorRef DataTypes.Response.types.mentionResponse (Json.serialize res)
                printf "mentionSearchCalled"
            
            | Request.allHashtagSearch ->
                let data = Json.deserialize<Request.searchAllHashTags> searchMaster.data
                let connection = new SQLiteConnection(connectionStringMemory)
                let res = SQLQueries.dbGetAllHashTag connection
                responseSend actorRef DataTypes.Response.types.allHashTagSearchResponse (Json.serialize res)
                
                
            | Request.userSearch ->
                let data = Json.deserialize<Request.searchAllUsers> searchMaster.data
                let connection = new SQLiteConnection(connectionStringMemory)
                let res = SQLQueries.dbGetAllUsers connection
                responseSend actorRef DataTypes.Response.types.allUserInfoResponse (Json.serialize res)
                
            | Request.tweetWithHashTagSearch ->
                let data = Json.deserialize<Request.searchTweetWithHashTagRequest> searchMaster.data
                let connection = new SQLiteConnection(connectionStringMemory)
                let res = SQLQueries.dbGetTweetWithTag data.hashtag connection
                responseSend actorRef DataTypes.Response.types.hashTagTweetsResponse (Json.serialize res)
                
            | _ ->
                printf "someError"
        
        
        
        return! loop()
    }
    loop()
    
let SearchActor = spawn serverSystem "SEARCH_ACTOR" followActor

let serverActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive ()
        let reqData = Json.deserialize<Request.masterData> message
        printf "%s" reqData.data
        match reqData.option with
        | Request.types.registerRequest ->
            //register user here
            printf "registering user here"
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender)
            
            RegistrationActor <! data
            
            
        | Request.types.loginRequest ->
            printf "login"
            let loginData = Json.deserialize<Request.loginRequest> reqData.data
            liveActorMap.Add(loginData.uid, mailBox.Context.Sender)
            responseSend mailBox.Context.Sender DataTypes.Response.types.loginResponse "loggedIn"
            
            
        | Request.types.logoutRequest ->
            printf "logout"
            let logoutData = Json.deserialize<Request.logoutRequest> reqData.data
            liveActorMap.Remove(logoutData.uid) |> ignore
            
        | Request.types.submitTweetRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender)
            
            TweetActor <! data
            
        | Request.types.submitReTweetRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender) 
            TweetActor <! data
            
        | Request.types.followRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender)
            printf "follow"
            FollowActor <! data
            
        | DataTypes.Request.types.followBulkRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender)
            printf "follow"
            FollowMassActor <! data
        | Request.types.feedRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender)
            FeedActor <! reqData.data
        
        | Request.types.searchRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, mailBox.Context.Sender)
            printf "search" // can be of multiple types
            SearchActor <! data
        
        | _ ->
            printf "someError"
        
        return! loop()
    }
    loop()



let serverStarter =
    spawn serverSystem "server" serverActor



let clientSystem = Client.system

//simulation

let createUserSimu userId password=
    let reg : DataTypes.Request.registerRequest = {
        uid = userId
        password = password
    }
    let data : DataTypes.Request.masterData = {
        option = Request.types.registerRequest
        data = Json.serialize reg
    }
    Json.serialize data
    
    
let createFollowSimu meId youId =
    let req : DataTypes.Request.followRequest = {
        uid = meId
        follow_id = youId
    }
    
    let data : DataTypes.Request.masterData = {
        option = Request.types.followRequest
        data = Json.serialize req
    }
    Json.serialize data
    
let loginSimu uid =
    let req : loginRequest = {
        uid = uid
        password = ""
    }
    let data : DataTypes.Request.masterData = {
        option = Request.types.loginRequest
        data = Json.serialize req
    }
    Json.serialize data


let tweetSimu tweetText uid=
    let tweet : DataTypes.simulator.tweetData = {
        tweet = tweetText
        uid = uid
        
    }
    
    let data : DataTypes.Request.masterData = {
        option = Request.types.submitTweetRequest
        data = Json.serialize tweet
    }
    Json.serialize data
    

let getFeed uid =
    let req : DataTypes.Request.feedRequest = {
        uid = uid
    }
    
    let data : DataTypes.Request.masterData = {
        option = Request.types.feedRequest
        data = Json.serialize req
    }
    Json.serialize data
//    
//let user1 = spawn clientSystem "1" Client.clientActor
//let user2 = spawn clientSystem "2" Client.clientActor
//
//user1 <! createUserSimu "1" "123"
//user2 <! createUserSimu "2" "123"
//Thread.Sleep(1000)
//user1 <! loginSimu "1"
//user2 <! loginSimu "2"
//Thread.Sleep(1000)
//user1 <! tweetSimu "First Tweet" "1"
//user2 <! tweetSimu "Second Tweet @1" "2"
//
//System.Console.ReadLine() |> ignore


//let's create user table


