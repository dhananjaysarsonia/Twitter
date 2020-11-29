module Twitter.Client

open System
open System.Collections
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Akka
open Akka.Actor
open Akka.Dispatch.SysMsg
open System.Collections.Generic
open Akka.FSharp
open Akka.Actor
open System.Diagnostics
open Akka.Util
open System.Threading;
open FSharp.Data
open FSharp.Json

module Data_types =
    type loginType = {
        uid : int
        password : string
    }
    
    type followType = {
        uid : int
        follow_id : int
    }
    
    type tweetType = {
        uid : int
        tweet_id : string
        mentions : int[]
        hashtags : string[]
    }
    
    type retweetType = {
        uid : int
        tweet_id : string
    }
    
    type search_hashtagType = {
       uid : int
       to_search : string
    }
    type search_mymentionType = {
        uid : int
    }
    
    type logoutType = {
        uid : int
    }



//type login = JsonProvider<"""{
//  "type": "login",
//  "uid":1
//}""">
//
//type follow = JsonProvider<"""{
//  "type":"follow",
//  "uid": 1,
//  "follow_id": 2
//}""">
//
//type tweet = JsonProvider<"""{
//  "type": "publish_tweet",
//  "uid": 12324,
//  "tweet_id": 233,
//  "mentions": [
//    1234,
//    455
//  ],
//  "hashtags": [
//    "lorem",
//    "ipsum"
//  ]
//}""">
//type retweet = JsonProvider<"""{
//    "type":"retweet",
//    "uid" : 1,
//    "tweet_id" : 2
//}""">
//
//type search = JsonProvider<"""{
//    "type" : "search",
//    "uid" : 1,
//    "to_search" : "lorem"
//}""">
//
//type logout = JsonProvider<"""{
//  "type": "logout",
//  "uid":1
//}""">

//Login
let Login userId =
    //    let mutable login_request = login.Parse("""{"type":"login","uid": userId}""")
    //    Twitter.Server.client_call login_request
    let jsonData : Data_types.loginType = { uid = userId ; password = ""}
    let data = Json.serialize jsonData
    Twitter.Server.client_call data


  
//Follow  
let Follow userId followId =
    //let mutable follow_request = follow.Parse("""{"type" : "follow","uid" : userId, "follow_id":followId}""")
    //Twitter.Server.client_call follow_request
    let jsonData : Data_types.followType = { uid = userId ; follow_id = followId }
    let data = Json.serialize jsonData
    Twitter.Server.client_call data
    
//Tweet    
let Tweet userId tweetId mentions_list hashtags_list =
    //let mutable tweet_publishrequest = tweet.Parse("""{"type": "publish_tweet", "uid" : userId, "tweet_id": tweetId, "mentions" : mentions_list, "hashtags" : hashtags_lists}""")
    //Twitter.Server.client_call tweet_publishrequest
    let jsonData : Data_types.tweetType = { uid = userId ; tweet_id = tweetId ; mentions = mentions_list; hashtags = hashtags_list }
    let data = Json.serialize jsonData
    Twitter.Server.client_call data
 
//Retweet    
let Retweet userId tweetId =
    //let mutable retweet_request = retweet.Parse("""{"type": "retweet" , "uid" : userId, "tweet_id": tweetId}""")
    //Twitter.Server.client_call retweet_request
    let jsonData : Data_types.retweetType = { uid = userId ; tweet_id = tweetId }
    let data = Json.serialize jsonData
    Twitter.Server.client_call data
    
//Search for Hashtag
let Search_hashtag userId hashTag =
    //let mutable search_hashtagrequest = search.Parse("""{"type" : "hashtag_search", "uid" : userId , "to_search" : hashTag}""")
    //Twitter.Server.client_call search_hashtagrequest
    let jsonData : Data_types.search_hashtagType = { uid = userId ; to_search = hashTag }
    let data = Json.serialize jsonData
    Twitter.Server.client_call data
    
//Search for User's mentions    
let Search_mymentions userId =
    //let mutable search_mentionrequest = search.Parse("""{"type" : "mymention_search", "uid" : userId}""")
    //Twitter.Server.client_call search_mentionrequest
    let jsonData : Data_types.search_mymentionType = { uid = userId }
    let data = Json.serialize jsonData
    Twitter.Server.client_call data
    
//Logout    
let Logout userId =
    //let mutable logout_request = logout.Parse("""{"type":"logout","uid": userId}""")
    //Twitter.Server.client_call logout_request
    let jsonData : Data_types.logoutType = { uid = userId }
    let data = Json.serialize jsonData
    Twitter.Server.client_call data