module Twitter.Client

open System.Collections.Generic
open System.Data.SQLite
open Akka.Actor
open FSharp.Data.JsonProvider
open FSharp.Json
open Twitter.DataTypes
open Twitter.DataTypes.Request
open Twitter.DataTypes.Response
open Akka.FSharp


let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 0
            }
        }"

let system = System.create "client" config

let server = system.ActorSelection("akka.tcp://twitterServer@localhost:9001/user/server")

let sendRequest option data =
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
    }
    server <! Json.serialize req


let clientActor(mailBox : Actor<_>) =
    let mutable uid = ""
    let rec loop() = actor{
        let! message = mailBox.Receive()
        let masterData = Json.deserialize<DataTypes.simulator.master> message
        
        match masterData.option with
        | DataTypes.Request.types.registerRequest ->
            let req = Json.deserialize<DataTypes.Request.registerRequest> masterData.data
            uid <- req.uid
            sendRequest DataTypes.Request.types.registerRequest masterData.data
            
            
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
            let mention: string[] = [||]
            let hashtag: string[] = [||]
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
             
            
            
            
            
        | DataTypes.Request.types.submitReTweetRequest ->
            printf ""
        
        
        | DataTypes.Request.types.followRequest ->
            printf ""
            
        | DataTypes.Request.types.feedRequest ->
            printf ""
        
        
        //responses
        
        
        
        return! loop()
    }
    loop()
