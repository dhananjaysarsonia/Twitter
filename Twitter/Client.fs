module Twitter.Client

open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Data.SQLite
open System.Text
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
open Server

printf "client"
let system = System.create "client" (Configuration.defaultConfig())

Async.Start Server.startServer

let ws = new ClientWebSocket()
let uri = new Uri("ws://localhost:8080/websocket")
let wcts = new CancellationToken()
let cts = new CancellationTokenSource()





//let server = system.ActorSelection("akka.tcp://serverSystem@localhost:9001/user/server")

let sendRequest option data uid=
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
        uid = uid
    }
    let jsonString = Json.serialize req
    let response = Http.Request("http://127.0.0.1:8080/api",httpMethod = "POST",headers = [ ContentType HttpContentTypes.Json ],body = TextRequest jsonString)
    match response.Body with
    | Text text ->
        text
    | _ ->
        "empty response"
    
      //server <! Json.serialize req

let reqMaker option data uid=
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
        uid = uid

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
//        while not task.IsCompleted do
//            ()
        task.Wait()
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


let mutable continueFlag = true
let mutable userId = ""
while continueFlag do
    Thread.Sleep(1000)
    printf "\nPlease enter command: \n"
    let command: string = Console.ReadLine()
    let commandArray = command.Split("/")
    match commandArray.[0] with
    | "register" ->
        let user = commandArray.[1]
        let password = commandArray.[2]
        let request: DataTypes.Request.registerRequest = {
            uid = user
            password = password
        }
        let response = sendRequest DataTypes.Request.types.registerRequest (Json.serialize request) user
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
        let response = sendRequest DataTypes.Request.types.loginRequest (Json.serialize request) user
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
        else
            printf "Authentication error"
    
    | "follow" ->
        let toFollow = commandArray.[1]
        let request: DataTypes.Request.followRequest = {
            uid = userId
            follow_id = toFollow
        }
        let response = sendRequest DataTypes.Request.types.followRequest (Json.serialize request) userId
        printf "%s \n" response
        
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
        let response = sendRequest DataTypes.Request.types.submitTweetRequest (Json.serialize tweetData) userId
        printf "%s \n" response
        
    | "mymention" ->
        let request : DataTypes.Request.searchMyMentionRequest = {
            uid = userId
        }
        let searchWrapper : DataTypes.Request.searchMaster = {
            option = Request.myMentionSearch
            data = Json.serialize(request)
        }
        let response = sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchWrapper) userId
        printf "%s \n" response
        
    | "hashtagsearch" ->
        let hashtag = commandArray.[1]
        let request : DataTypes.Request.searchTweetWithHashTagRequest = {
            uid = userId
            hashtag = hashtag
        }
        let searchWrapper : DataTypes.Request.searchMaster = {
            option = Request.tweetWithHashTagSearch
            data = Json.serialize(request)
        }
        let response = sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchWrapper) userId
        printf "%s \n" response
    | "retweet" ->
        let tweetId = commandArray.[1]
        let request : DataTypes.Request.tweetSubmitRequest = {
            tweet = ""
            tweetId = tweetId
            uid = userId
            mentions = [||]
            hashtags = [||]
            isRetweet = true
            origOwner = ""
        }
        let response = sendRequest DataTypes.Request.types.submitReTweetRequest (Json.serialize request) userId
        printf "%s \n" response
    | "feed" ->
        let data: Request.feedRequest = {
            uid = userId
        }
        let response = sendRequest DataTypes.Request.types.feedRequest (Json.serialize data) userId
        printf "%s \n" response
        
        
    | "logout" ->
        continueFlag <- false
    
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