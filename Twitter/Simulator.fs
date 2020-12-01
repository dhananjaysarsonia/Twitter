module Twitter.Simulator

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
open Twitter.Client.Data_types

let numUsers = 1000
let mutable tweet_id = 0

let system = System.create "system" (Configuration.defaultConfig())

type Message =
    | Register
    | Login
    | Follow of int*int
    | Tweet
    | Retweet
    | Search
    | Logout
    | Response
   
let random = new System.Random()

let simulator(mailbox : Actor<_>) =
    let mutable regUsers = 100
    let mutable currentusercount = 0
    let rec loop () = actor {
                let! message = mailbox.Receive ()
                match message with
                | Register ->
                    while regUsers < numUsers do
                        for i in currentusercount .. regUsers-1 do
                            Client.Login i
                        let mutable catone_users = 20 * regUsers / 100
                        let mutable cattwo_users = 80 * regUsers / 100
                        
                        // Right after registering Follow somebody
                        mailbox.Self <! Follow catone_users cattwo_users
                        
                        regusers <- regusers + 100
                        currentusercount <- currentusercount + 100
                        
                        //Without making the actor sleep, pause registering for 3 seconds and resume with new 100 users.
                        //Thread.Sleep(300)
                        
                //| Login ->
                    //Randomly login 10% of inactive users every __ seconds
                    
                | Follow category_one category_two ->
                    
                    //Follow request to client For Category One Users (20% users having 80% followers)
                    for i in 1 .. category_one do
                        for j in 1 .. 80 do
                            //Currently follow_id is randomly selected but ideally should be checked if they already follow each other (uniqueness)
                            let mutable follow_id = random.Next(regUsers)
                            Client.Follow i follow_id
                                    
                    //Follow request to client For Category Two Users (80% users having 20% followers)
                    for i in 1 .. category_two do
                         for j in 1 .. 20 do
                            //Currently follow_id is randomly selected but ideally should be checked if they already follow each other (uniqueness)
                            let mutable follow_id = random.Next(regUsers)
                            Client.Follow i follow_id
                            
                | Tweet ->
                    
                    
                //| Logout ->
                    //Randomly logout 10% of active users every __ seconds
    }
    
let simulator_actor = spawn system "simulator" simulator
simulator_actor <! Register

                  
    //Tweet        
    let Tweet_catone catone_users =
        
        //Category One users will tweet more frequently (sleep time of 3 seconds?)
        //Randomly select mentions from the userId table
        //Maintain a dictionary of hashtags and randomly select hashtags from it
        for i in 1 .. catone_users do
            tweet_id <- tweet_id + 1
            let mutable tweettopublish = tweet.Parse("""{"uid": i, "tweetId": tweet_id, "mentions": [ 1234,455], "hashtags": [ "lorem", "ipsum"]}""")
            Twitter.Server.publishtweet_actor tweettopublish
            Thread.Sleep(300)
    
    let Tweet_cattwo cattwo_users =        
        //Category Two users will tweet less frequently (sleep time of 8 seconds?)
        //Randomly select mentions from the userId table
        //Maintain a dictionary of hashtags and randomly select hashtags from it
        for i in 1 .. cattwo_users do
            tweet_id <- tweet_id + 1
            let mutable tweettopublish = tweet.Parse("""{"uid": i, "tweetId": tweet_id, "mentions": [ 1234,455], "hashtags": [ "lorem", "ipsum"]}""")
            Twitter.Server.publishtweet_actor tweettopublish
            Thread.Sleep(800)
        
    
    //Retweet
    let ReTweet_catone catone_users =
        //Category One users will retweet more frequently (sleep time of 3 seconds?)
        //Randomly select tweets from the users the user Follows
        for i in 1 .. catone_users do
            let mutable retweet_request = tweet.Parse("""{"uid": i, "tweetId": 1234}""")
            Twitter.Server.retweet_actor retweet_request
            Thread.Sleep(300)
    
    let ReTweet_cattwo cattwo_users =        
        //Category Two users will retweet less frequently (sleep time of 8 seconds?)
        //Randomly select tweets from the users the user Follows
        for i in 1 .. cattwo_users do
            let mutable retweet_request = tweet.Parse("""{"uid": i, "tweetId": 1234}""")
            Twitter.Server.retweet_actor retweet_request
            Thread.Sleep(800)
    
    
    //Search
    
    //Login
    let Login userId =
        let mutable login_request = login.Parse("""{"uid" : userId}""")
        Twitter.Server.login_actor login_request