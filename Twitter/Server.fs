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

open System
open System.Collections.Generic
open System.Configuration
open System.Data.SQLite
open System.Data.SQLite
open System.Data.SQLite
open System.Net.WebSockets
open System.Threading
open Akka.Actor
open FSharp.Data.JsonProvider
open FSharp.Json
open Twitter.DataTypes
open Twitter.DataTypes.Request
open Twitter.DataTypes.Response
open Akka.FSharp
open Twitter.DataTypes.simulator

open Suave.ServerErrors
open Suave.Writers
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket


//let serverSetup = 

let databaseFilename = "/Users/dhananjaysarsonia/RiderProjects/Twitter/Twitter/tweeeeterdb"
//let databaseFilename = "tweeeeterdb"
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
 
let serverSystem = System.create "serverSystem" (Configuration.defaultConfig())


type ServerDataWrapper = 
    |Request of string*WebSocket
    |Init 


let mutable liveActorMap = new Dictionary<string, WebSocket>()

let responseSend (actor : Suave.WebSocket.WebSocket) (option : string) (data : string) =
    let response : Response.masterData = {
        option = option
        data = data
    }
    let res = Json.serialize response
    let byteResponse =
      res
      |> System.Text.Encoding.ASCII.GetBytes
      |> ByteSegment
    Async.RunSynchronously(actor.send Text byteResponse true) |> ignore
 
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
            printf "in feed"
            
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
            //connection.Open()
            for i in followers.rows do
                SQLQueries.dbInsertFeed i.userId tweet.tweetId tweet.tweet tweet.uid tweet.isRetweet tweet.origOwner connection
                if liveActorMap.ContainsKey i.userId then
                    responseSend (liveActorMap.Item(i.userId))  (DataTypes.Response.types.sendTweetInFeed)  (Json.serialize tweet)
            
           // connection.Close()
            
            responseSend actorRef DataTypes.Response.types.sendTweetResponse (Json.serialize tweet)
            
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
            let timestamp = string <| Guid.NewGuid()
            let tweetIdGen = tweet.uid + timestamp
            if not tweet.isRetweet then 
                
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
    //            tweet.tweetId = tweetIdGen
                //connection.Open()
                SQLQueries.dbInsertTweet tweetIdGen tweet.tweet tweet.uid tweet.isRetweet tweet.origOwner connection
                
                //if not a retweet we need to put mentions and hash tags
                if not tweet.isRetweet then
                    for hashtag in tweet.hashtags do
                        SQLQueries.dbInsertHashTag hashtag connection
                        SQLQueries.dbInsertHashTagForTweet hashtag tweet.uid tweetIdGen tweet.tweet connection
                    for mention in tweet.mentions do
                        SQLQueries.dbInsertMention tweetIdGen tweet.tweet mention tweet.uid connection
                
                UpdateFeedActor <! ServerDataWrapper.Request((Json.serialize tweetForFeed), actorRef)
            else
                let reqOwner = tweet.uid
                let origTweetId = tweet.tweetId
                let origTweet = SQLQueries.dbGetTweetFotTweetIdWithConnectionNotOpen origTweetId connection
                SQLQueries.dbInsertTweet tweetIdGen origTweet.tweet reqOwner true origTweet.uid connection
                let updatedTweet = SQLQueries.dbGetTweetFotTweetIdWithConnectionNotOpen tweetIdGen connection
                
                let tweetForFeed : Request.tweetSubmitRequest = {
                    tweet = updatedTweet.tweet
                    tweetId = updatedTweet.tweetId
                    //flag = tweet.flag
                    isRetweet = updatedTweet.flag
                    mentions = [||]
                    hashtags = [||]
                    uid = updatedTweet.tweetId
                    origOwner = updatedTweet.origTweetId
                    
                }
                
                
                UpdateFeedActor <! ServerDataWrapper.Request((Json.serialize tweetForFeed), actorRef)
               
                
                
                    
          //  connection.Close()
            
 
//********************************  call feed actor here
           
            
            
           // responseSend actorRef DataTypes.Response.types.sendTweetResponse "OK"

            

    
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
//        printf "*********************Heree*************************** \n"
//        printf "%A" message
        match message with
        | Init ->
            printf "Felt Cute, might remove later"
        | ServerDataWrapper.Request(data, actorRef) ->
            let res = Json.deserialize<DataTypes.simulator.followBulkData> data
            let connection = new SQLiteConnection(connectionStringMemory)
           // connection.Open()
            for rowData in res.followList do
                SQLQueries.dbInsertFollow res.uid (string <| rowData) connection
            
            
            //connection.Close()
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
//            printf "REACHED IN SEARCH ACTOR****************************** \n \n \n"
            let searchMaster = Json.deserialize<Request.searchMaster> data
          //  printf "%s" searchMaster.data
               
            match searchMaster.option with
            | Request.myMentionSearch ->
                let data = Json.deserialize<Request.searchMyMentionRequest> searchMaster.data
                let connection = new SQLiteConnection(connectionStringMemory)
                let res = SQLQueries.dbGetMentionsOfUser data.uid connection
                responseSend actorRef DataTypes.Response.types.mentionResponse (Json.serialize res)
          //      printf "mentionSearchCalled"
            
//            | Request.allHashtagSearch ->
//                let data = Json.deserialize<Request.searchAllHashTags> searchMaster.data
//                let connection = new SQLiteConnection(connectionStringMemory)
//                let res = SQLQueries.dbGetAllHashTag connection
//                responseSend actorRef DataTypes.Response.types.allHashTagSearchResponse (Json.serialize res)
//                
//                
//            | Request.userSearch ->
//                let data = Json.deserialize<Request.searchAllUsers> searchMaster.data
//                let connection = new SQLiteConnection(connectionStringMemory)
//                let res = SQLQueries.dbGetAllUsers connection
//                responseSend actorRef DataTypes.Response.types.allUserInfoResponse (Json.serialize res)
                
            | Request.tweetWithHashTagSearch ->
                let data = Json.deserialize<Request.searchTweetWithHashTagRequest> searchMaster.data
                let connection = new SQLiteConnection(connectionStringMemory)
                
                let res = SQLQueries.dbGetTweetWithTag data.hashtag connection
                responseSend actorRef DataTypes.Response.types.hashTagTweetsResponse (Json.serialize res)
                
            | _ ->
                printf "someError \n"
        
        
        
        return! loop()
    }
    loop()
    
let SearchActor = spawn serverSystem "SEARCH_ACTOR" searchActor

let serverActor(mailBox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive ()
        let reqData = Json.deserialize<Request.masterData> message
        
       // printf "%s" reqData.data
        match reqData.option with
//        | Request.types.registerRequest ->
//            //register user here
//           // printf "registering user here"
//            let data  = ServerDataWrapper.Request(reqData.data, liv)
//            
//            RegistrationActor <! data
//            
            
        | Request.types.loginRequest ->
          //  printf "login"
            let loginData = Json.deserialize<Request.loginRequest> reqData.data
            //liveActorMap.Add(loginData.uid, liveActorMap.[reqData.uid])
            responseSend (liveActorMap.[reqData.uid]) DataTypes.Response.types.loginResponse "loggedIn"
            
            
        | Request.types.logoutRequest ->
          //  printf "logout"
            let logoutData = Json.deserialize<Request.logoutRequest> reqData.data
            liveActorMap.Remove(logoutData.uid) |> ignore
            
        | Request.types.submitTweetRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, liveActorMap.[reqData.uid])
            
            TweetActor <! data
            
        | Request.types.submitReTweetRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, liveActorMap.[reqData.uid]) 
            TweetActor <! data
            
        | Request.types.followRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, liveActorMap.[reqData.uid])
         //   printf "follow"
            FollowActor <! data
            
        | DataTypes.Request.types.followBulkRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, liveActorMap.[reqData.uid])
        //    printf "follow"
            FollowMassActor <! data
        | Request.types.feedRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, liveActorMap.[reqData.uid])
            FeedActor <! data
        
        | Request.types.searchRequest ->
            let data  = ServerDataWrapper.Request(reqData.data, liveActorMap.[reqData.uid])
//            printf "*****************SEARCH*****************************"
//            printf "SEARCH DATA %s \n" reqData.data
            //printf "search" // can be of multiple types
            SearchActor <! data
        
        | _ ->
            printf "someError"
        
        return! loop()
    }
    loop()



let serverStarter =
    spawn serverSystem "server" serverActor



    
let ws (webSocket : WebSocket) (context: HttpContext) =
  socket {
    let mutable loop = true
    printfn "Socket Connected"
    while loop do
      let! msg = webSocket.read()
      match msg with
      | (Text, data, true) ->
        let message = UTF8.toString data
        let loginData = Json.deserialize<Request.loginRequest> message
        liveActorMap.Add(loginData.uid, webSocket)

      | (Close, _, _) ->
        printfn "C%A" Close
        let emptyResponse = [||] |> ByteSegment
        do! webSocket.send Close emptyResponse true
        loop <- false

      | _ -> printfn "Matched Nothing %A" msg
    }
  
  
//let reqHandler   
      
      
let parseRequestAndSendProcessing(byteData : byte[]) =
    let message = System.Text.Encoding.UTF8.GetString(byteData)
    let request = Json.deserialize<Request.masterData> message
    match request.option with 
    | Request.types.loginRequest ->
        let connection = new SQLiteConnection(connectionStringMemory)
        let loginData = Json.deserialize<Request.loginRequest> request.data    
        let flag = SQLQueries.dbCheckLogin loginData.uid loginData.password connection
        if flag then
            "ok"
        else
            "Authentication failed"
        //connection.Dispose()
        
    | Request.types.registerRequest ->
        let connection = new SQLiteConnection(connectionStringMemory)
        let registerData = Json.deserialize<Request.registerRequest> request.data
        SQLQueries.dbAddNewUser registerData.uid registerData.password connection
        connection.Dispose()
        "ok"
    | _ ->
        serverStarter <! message
        "ok"

let handleApiRequest =
  request (fun req ->
  req.rawForm
  |> parseRequestAndSendProcessing
  |> OK )
  >=> setMimeType "application/json"  
      
let app : WebPart =
    choose[
        path "/" >=> OK "Success"
        path "/websocket" >=> handShake ws
        path "/api" >=> POST >=> handleApiRequest
        NOT_FOUND "error 404"

       
    ]

let startServer =
    async{
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    }


    