module Twitter.Client

open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Data.SQLite
open Akka.Actor
open FSharp.Data.JsonProvider
open FSharp.Json
open Twitter
open Twitter.DataTypes
open Twitter.DataTypes.Request
open Twitter.DataTypes.Request
open Twitter.DataTypes.Request
open Twitter.DataTypes.Request
open Twitter.DataTypes.Response
open Akka.FSharp
open Twitter.DataTypes.simulator
open System.Net.WebSockets
open FSharp.Data.HttpRequestHeaders
open FSharp.Data
open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 0
            }
        }"
printf "client"
let system = System.create "client" (Configuration.defaultConfig())



let ws = new ClientWebSocket()
let uri = new Uri("ws://localhost:8080/websocket")
let wcts = new CancellationToken()
let cts = new CancellationTokenSource()





//let server = system.ActorSelection("akka.tcp://serverSystem@localhost:9001/user/server")

let sendRequest option data =
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
    }
    let jsonString = Json.serialize req
    let response = Http.Request("http://127.0.0.1:8080/api",httpMethod = "POST",headers = [ ContentType HttpContentTypes.Json ],body = TextRequest jsonString)
    match response.Body with
    | Text text ->
        text
    | _ ->
        "empty response"
    
      //server <! Json.serialize req

let reqMaker option data =
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
    }
    Json.serialize req

//making empty feed for initialization
let nullFeedList : list<DataTypes.Response.feed_row> = []
let nullFeed : DataTypes.Response.feeds = {
    uid = ""
    rows = nullFeedList
}


let rec socketHandler () =
    async {
        let segment = new ArraySegment<Byte>( Array.create (1500) Byte.MinValue)
        let task =  ws.ReceiveAsync(segment,wcts)
        while not (task.IsCompleted) do
            ()
        let response = System.Text.Encoding.ASCII.GetString (segment.Array)
        printfn "\n Server Response%s" response
        return! socketHandler()
    }

let parseTweet (tweet:string) =
            let mutable hashtags = []
            let mutable mentions = []
            let words = tweet.Split ' '
            for word in words do
                if word.StartsWith("#") then
                    hashtags <- hashtags @ [word.[1..]]
                if word.StartsWith("@") then
                    mentions <- mentions @ [word.[1..]]
            ( List.toArray(hashtags),mentions |> List.toArray)

let clientActor(mailBox : Actor<_>) =
    let mutable uid : string = ""
    let mutable count = 0
    let mutable feed: DataTypes.Response.feeds = nullFeed 
    let rec loop() = actor{
        let! message = mailBox.Receive()
        let masterData = Json.deserialize<DataTypes.simulator.master> message
        
        match masterData.option with
        | DataTypes.Request.types.registerRequest ->
            let req = Json.deserialize<DataTypes.Request.registerRequest> masterData.data
            uid <- string <| req.uid
            printf "%s" (sendRequest DataTypes.Request.types.registerRequest masterData.data)
            
    
                                 
            
        | DataTypes.Request.types.loginRequest ->
            let req = Json.deserialize<DataTypes.Request.loginRequest> masterData.data
            uid <- req.uid
            let res = sendRequest DataTypes.Request.types.loginRequest masterData.data
            if(res = "ok") then
                let task = ws.ConnectAsync(uri, wcts)
                while not(task.IsCompleted) do
                    () //nothing
                Async.Start(socketHandler(), cancellationToken = cts.Token)
                //toDo
                //push userId in socket
                
                
                
                   
            
            
        | DataTypes.Request.types.logoutRequest ->
            let req = Json.deserialize<DataTypes.Request.logoutRequest> masterData.data
            uid <- req.uid
            sendRequest DataTypes.Request.types.logoutRequest masterData.data
            
            
        | DataTypes.Request.types.submitTweetRequest ->
            let rawTweet = Json.deserialize<DataTypes.simulator.tweetData> masterData.data
            //*****************************************
            //NEED TO PARSE ARRAYS
            let hashtag, mention = parseTweet rawTweet.tweet
//            let mention: string[] = [||]
//            let hashtag: string[] = [||]
            let tweetData : DataTypes.Request.tweetSubmitRequest = {
                tweet = rawTweet.tweet
                tweetId = ""
                uid = rawTweet.uid
                mentions = mention
                hashtags = hashtag
                isRetweet = false
                origOwner = ""
            }
            sendRequest DataTypes.Request.types.submitTweetRequest (Json.serialize tweetData)
             
            
        | DataTypes.Request.types.submitReTweetRequest  ->
            let feedData = feed.rows
            
            if feedData.Length <> 0 then
                let random = new System.Random()
                let index = random.Next(0, feedData.Length)
                let row = feedData.[index]
                
                let tweetData : DataTypes.Request.tweetSubmitRequest = {
                    tweetId = ""
                    tweet = row.tweet.tweet
                    isRetweet = true
                    uid = uid
                    mentions = [||]
                    hashtags = [||]
                    origOwner = row.uid
                    
                }
                sendRequest DataTypes.Request.types.submitReTweetRequest (Json.serialize tweetData)
                
               
                
                 
        
        | DataTypes.Request.types.followRequest ->
            sendRequest DataTypes.Request.types.followRequest masterData.data
            
        | DataTypes.Request.types.followBulkRequest ->
            sendRequest DataTypes.Request.types.followBulkRequest masterData.data
//            let followData = Json.deserialize<DataTypes.Request.followRequest> masterData.data

            
            
        | DataTypes.Request.types.feedRequest ->
            let reqData : DataTypes.Request.feedRequest = {
                uid = uid
            }
            sendRequest DataTypes.Request.types.feedRequest (Json.serialize reqData)

        | DataTypes.Request.types.hashTagTweetRequest ->
            //search for hashtag
            let hashtag = Json.deserialize<DataTypes.Request.searchTweetWithHashTagRequest> masterData.data
            let searchMaster : DataTypes.Request.searchMaster = {
                option = DataTypes.Request.tweetWithHashTagSearch
                data = string <| masterData.data
            }
            
            sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchMaster)
            
            
            printf ""
        | DataTypes.Request.types.mentionRequest ->
            let hashtag = Json.deserialize<DataTypes.Request.searchMyMentionRequest> masterData.data
            let searchMaster : DataTypes.Request.searchMaster = {
                option = DataTypes.Request.myMentionSearch
                data = string <| masterData.data
            }
            sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchMaster)
            
            
      
        //responses
            
            
        | _ ->
            count <- count - 1
            if count <= 0 then
                mailBox.Self <! reqMaker DataTypes.DONEString " "
            printf "error \n"
        
        return! loop()
    }
    loop()
    

let mutable continueFlag = true
let mutable userId = ""
while continueFlag do
    printf "Please enter command \n"
    let command: string = Console.ReadLine()
    let commandArray = command.Split(" ")
    match commandArray.[0] with
    | "signup" ->
        let user = commandArray.[1]
        let password = commandArray.[2]
        let request: DataTypes.Request.registerRequest = {
            uid = user
            password = password
        }
        let response = sendRequest DataTypes.Request.types.registerRequest (Json.serialize request)
        if response.Equals("ok") then
            printf "User registered"
        else
            printf "Invalid"
    
    | "login" ->
        let user = commandArray.[1]
        let password = commandArray.[2]
        let request: DataTypes.Request.loginRequest = {
            uid = user
            password = password
        }
        let response = sendRequest DataTypes.Request.types.loginRequest (Json.serialize request)
        if response.Equals("ok") then
            printf "User logged in"
            userId <- user
            let task = ws.ConnectAsync(uri, wcts)
            while not (task.IsCompleted) do
                ()
            Async.Start(socketHandler(), cancellationToken = cts.Token)
            let socketMessage = Json.serialize request
            let byteMessage = System.Text.Encoding.ASCII.GetBytes socketMessage
            let segment = new ArraySegment<byte> (byteMessage)
            let task = ws.SendAsync(segment, WebSocketMessageType.Text, true, wcts)
            printf "Socket created"
    
    | "follow" ->
        let toFollow = commandArray.[1]
        let request: DataTypes.Request.followRequest = {
            uid = userId
            follow_id = toFollow
        }
        let response = sendRequest DataTypes.Request.types.followRequest (Json.serialize request)
        printf "%s" response
        
    | "tweet" ->
        let rawTweet = commandArray.[1]
        let hashtag, mention = parseTweet rawTweet
//            let mention: string[] = [||]
//            let hashtag: string[] = [||]
        let tweetData : DataTypes.Request.tweetSubmitRequest = {
            tweet = rawTweet
            tweetId = ""
            uid = userId
            mentions = mention
            hashtags = hashtag
            isRetweet = false
            origOwner = ""
        }
        let response = sendRequest DataTypes.Request.types.submitTweetRequest (Json.serialize tweetData)
        printf "%s" response
        
    | "mymention" ->
        let request : DataTypes.Request.searchMyMentionRequest = {
            uid = userId
        }
        let searchWrapper : DataTypes.Request.searchMaster = {
            option = Request.types.mentionRequest
            data = Json.serialize(request)
        }
        let response = sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchWrapper)
        printf "%s" response
        
    | "hashtagsearch" ->
        let hashtag = commandArray.[1]
        let request : DataTypes.Request.searchTweetWithHashTagRequest = {
            uid = userId
            hashtag = hashtag
        }
        let searchWrapper : DataTypes.Request.searchMaster = {
            option = Request.types.hashTagTweetRequest
            data = Json.serialize(request)
        }
        let response = sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchWrapper)
        printf "%s" response
        
        
    | _ ->
        printf "cannot recognize the command, please try again"
    
    
    
        
//            
//    | "tweet" ->
//        let rawTweet = command.[1]
//        let data = 
//    
   
           
    
        



//
//    |DataTypes.Request.types.loginRequestBulk ->
//            let req = Json.deserialize<DataTypes.Request.loginWithActionsRequest> masterData.data
//            uid <- req.uid
//            count <- req.actionList.Length
//            for action in req.actionList do
//                let desAction = Json.deserialize<DataTypes.simulator.master> action
//                match desAction.option with
//                | DataTypes.Request.types.submitTweetRequest ->
//                    let rawTweet = desAction.data
//                    let tweet : DataTypes.simulator.tweetData = {
//                        uid = uid
//                        tweet = rawTweet
//                    }
//                    mailBox.Self <! reqMaker DataTypes.Request.types.submitTweetRequest (Json.serialize tweet) 
//                | DataTypes.Request.types.submitReTweetRequest ->
//                    mailBox.Self <! action
//                | DataTypes.Request.types.mentionRequest ->
//                    let data : DataTypes.Request.searchMyMentionRequest = {
//                        uid = uid
//                    }
//                    mailBox.Self <! reqMaker DataTypes.Request.types.mentionRequest (Json.serialize data)
//                | DataTypes.Request.types.hashTagTweetRequest ->
//                    let tag = desAction.data
//                    let data : DataTypes.Request.searchTweetWithHashTagRequest = {
//                        uid = uid
//                        hashtag = tag
//                    }
//                    mailBox.Self <! reqMaker DataTypes.Request.types.hashTagTweetRequest (Json.serialize data)
//                | _ ->
//                    printf "Some unexpected error occurred"
//                