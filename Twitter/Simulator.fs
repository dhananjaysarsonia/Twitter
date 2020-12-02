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
open Twitter.DataTypes.simulator

let totalUsers = 1000

let system = System.create "system" (Configuration.defaultConfig())

type Message =
    | Start
    | BeginProcess
    | Done

   
let random = new System.Random()

//To track active-inactive states of users
let active_state = Array.zeroCreate totalUsers

// Create a list of hashtags
let hashtag_list = []

let simulator(mailbox : Actor<_>) =
        let! message = mailbox.Receive()
        match message with
        | Start ->
            //Register
            //for i in 1..numUsers do
                //Send client an message to register with uid = i
            //All users are registered
            // Send actions_actor <! BeginProcess
            
        //| Done ->
            //Terminate                
                
let actions_actor(mailbox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailBox.Receive()
        match message with
        | BeginProcess ->
            //Login-Logout
            //Randomly select 50 actors from inactive state , spawn actors for the 50 users, send login request to client, update state to active
            //Send a message to begin activities
            //Each actor, after finishing the activities(count decided by category), receives a message and Logout, change active state and Kill the actor.
            //Repeat n times and send done message to the simulator.
            
        //Actions to be performed after login
        // 50 active actors will randomly select (1-5) and perform actions (?????)
        //{
            // 1-Follow
            // 1 day/Cycle/Time{
                //For the 50 actors logged-in, first actor will have 49 followers, second actor will have 25 actors,so on
                // user1 - n-1 - 49
                // user2 - n/2 - 25
                // user3 - n/3 - 17
                // user4 - n/4 - 13
                // user5 - n/5 - 10 ..... user35 - n/35 - 1 ...user50 - n/50- 1 followers
            //}            
            
            // 2-Tweet
                // 1 day/Cycle/Time{
                    // For 50 actors logged-in , first actor will do 25 tweets, second actor will do 17 tweets,so on
                    // user1 - n/2 - 25
                    // user2 - n/3 - 17
                    // user3 - n/4 - 13
                    // user4 - n/5 - 10 ..... user35 - n/35 - 1 ...user50 - n/50- 1 tweet
                
                    //Tweet Request - " Blah Blah Blah @user_id Blah Blah Blah Blah #hashtag #hashtag" where user_id is randomly selected from numUsers and hashtag is randomly selected from hashtags_list
                //}
                
            // 3-Retweet
                 // 1 day/Cycle/Time{
                    // For 50 actors logged-in , first actor will do 25 retweets, second actor will do 17 retweets,so on
                    // user1 - n/2 - 25
                    // user2 - n/3 - 17
                    // user3 - n/4 - 13
                    // user4 - n/5 - 10 ..... user35 - n/35 - 1 ...user50 - n/50- 1 retweet
                
                    //Retweet_id randomly selected from tweet_ids(????)
                //}
                
            // 4-Search
                // 1 day/Cycle/Time{
                    // For 50 actors logged-in , first actor will do 10 searches, second actor will do 5 retweets,so on
                    // user1 - n/5 - 10
                    // user2 - n/10 - 5
                    // user3 - n/15 - 3
                    // user4 - n/20 - 3 ..... user35 - n/35 - 0 ...user50 - n/50- 0 search
                
                    //Search type randomly selected between search_hashtag and search_my_mentions
                        // search hashtag - randomly select a hashtag from hashtags_list
                        // search my_mentions -  send userid
                //}
                  
                
        //}
        
    }


//let simulator_actor = spawn system "simulator" simulator
//simulator_actor <! Start
