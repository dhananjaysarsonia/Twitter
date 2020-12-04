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
let system = System.create "client" config

let server = system.ActorSelection("akka.tcp://serverSystem@localhost:9001/user/server")

let sendRequest option data =
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
    }
    server <! Json.serialize req

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
    let mutable uid = ""
    let mutable feed: DataTypes.Response.feeds = nullFeed 
    let rec loop() = actor{
        let! message = mailBox.Receive()
        let masterData = Json.deserialize<DataTypes.simulator.master> message
        
        match masterData.option with
        | DataTypes.Request.types.registerRequest ->
            let req = Json.deserialize<DataTypes.Request.registerRequest> masterData.data
            uid <- req.uid
            sendRequest DataTypes.Request.types.registerRequest masterData.data
            
        |DataTypes.Request.types.loginRequestBulk ->
            let req = Json.deserialize<DataTypes.Request.loginWithActionsRequest> masterData.data
            uid <- req.uid
            for action in req.actionList do
                let desAction = Json.deserialize<DataTypes.simulator.master> action
                match desAction.option with
                | DataTypes.Request.types.submitTweetRequest ->
                    let rawTweet = desAction.data
                    let tweet : DataTypes.simulator.tweetData = {
                        uid = uid
                        tweet = rawTweet
                    }
                    mailBox.Self <! reqMaker DataTypes.Request.types.submitTweetRequest (Json.serialize tweet) 
                | DataTypes.Request.types.submitReTweetRequest ->
                    mailBox.Self <! action
                | DataTypes.Request.types.mentionRequest ->
                    let data : DataTypes.Request.searchMyMentionRequest = {
                        uid = uid
                    }
                    mailBox.Self <! reqMaker DataTypes.Request.types.mentionRequest (Json.serialize data)
                | DataTypes.Request.types.hashTagTweetRequest ->
                    let tag = desAction.data
                    let data : DataTypes.Request.searchTweetWithHashTagRequest = {
                        uid = uid
                        hashtag = tag
                    }
                    mailBox.Self <! reqMaker DataTypes.Request.types.hashTagTweetRequest (Json.serialize data)
                | _ ->
                    printf "Some unexpected error occurred"
                    
        
            
        | DataTypes.Request.types.loginRequest ->
            let req = Json.deserialize<DataTypes.Request.loginRequest> masterData.data
            uid <- req.uid
            sendRequest DataTypes.Request.types.loginRequest masterData.data

            
            
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
                option = DataTypes.Request.types.hashTagTweetRequest
                data = masterData.data
            }
            
            sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchMaster)
            
            
            printf ""
        | DataTypes.Request.types.mentionRequest ->
            let hashtag = Json.deserialize<DataTypes.Request.searchMyMentionRequest> masterData.data
            let searchMaster : DataTypes.Request.searchMaster = {
                option = DataTypes.Request.types.mentionRequest
                data = masterData.data
            }
            sendRequest DataTypes.Request.types.searchRequest (Json.serialize searchMaster)
            
            
      
        //responses
        
        
        
        |DataTypes.Response.types.registerResponse ->
            printf "Register response: %s" masterData.data
            
        |DataTypes.Response.types.loginResponse ->
            printf "Login Response: %s" masterData.data
        
        |DataTypes.Response.types.sendTweetResponse ->
            printf "Send Tweet response: %s" masterData.data
            
        |DataTypes.Response.types.followersResponse ->
            printf "get followers response %s" masterData.data
            
        |DataTypes.Response.types.followResponse ->
            printf "follow response: %s" masterData.data
            
        |DataTypes.Response.types.feedResponse ->
            let resData = Json.deserialize<DataTypes.Response.feeds> masterData.data
            
            feed <- resData
            printf "Feed Response : %s" masterData.data
           
            
        |DataTypes.Response.types.sendTweetInFeed ->
            
            printf "Dynamic feed update feed update for liveActor : %s" masterData.data
            
        |DataTypes.Response.types.hashTagTweetsResponse ->
            printf "tweet with hashtag search response: %s" masterData.data
        
        |DataTypes.Response.types.allHashTagSearchResponse ->
            printf "all hashtag response %s" masterData.data
            
        |DataTypes.Response.types.mentionResponse ->
            printf "mention response %s" masterData.data
            
        | _ ->
            printf "error"
        
        return! loop()
    }
    loop()
    
