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
open FSharp.Json
open Twitter.DataTypes

let totalUsers = 100

let system = System.create "system" (Configuration.defaultConfig())

type Message =
    | Start
    | BeginProcess
    | Tweet of string
    | Retweet of string
    | Search of string
    | Logout of string
    | Done

let containsNumber number list = List.exists (fun elem -> elem = number) list 
let random = new System.Random()

//To track active-inactive states of users
let active_state = Array.zeroCreate totalUsers
let activity_count = Array.zeroCreate totalUsers
let mutable logout_count = 0
let mutable cycle_count = 0

// Create a list of hashtags
let hashtag_list = ["#1"; "#2"; "#3"; "#4"; "#5"; "#6"; "#7"; "#8"; "#9"; "#10"]

let simulator(mailbox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailbox.Receive()
        match message with
        | Start ->
            //Register
            let mutable data = string 1
            for i in 1 .. totalUsers do
                //Send client an message to register with uid = i
                
                //spawn totalUsers number of actors
                data <- string i 
                Client.sendRequest "registerRequest" data
            //All users are registered
            
            //Follow
            // Sends list of followers 
            let mutable fcount = 0
            for i in 1 .. totalUsers do
                let mutable follower_list = []
                fcount <- totalUsers/i
                activity_count.[i] <- totalUsers/i
                if fcount = totalUsers then
                    fcount <- fcount - 1
                    activity_count.[i] <-activity_count.[i]-1
                for i in 1 .. fcount do
                    let mutable temp = random.Next(1,totalUsers+1)
                    while containsNumber temp follower_list && containsNumber i follower_list do
                        temp <- random.Next(1,totalUsers+1)
                    follower_list <- follower_list @ [temp]
                let jsonData :  DataTypes.Request.followRequest = { uid = string i ; follow_list = follower_list}
                let data = Json.serialize jsonData
                Client.sendRequest "followRequest" data 
            mailbox.Self <! BeginProcess
        
        | BeginProcess ->
            
            let mutable n = 0
            let mutable data = string n
            for i in 1 .. 50 do    
                while active_state.[n] = 1 do
                    n <- random.Next(1,totalUsers+1)
                //send login request to client
                
                //To create a list of actions
                let mutable action_list = []
                for i in 1 .. activity_count.[n] do
                    let mutable temp = random.Next(1,4)
                    action_list <- action_list @ [temp]
                let jsonData :  DataTypes.Request.loginRequest = { uid = string i ; password = " " ; actions_list = action_list}
                let data = Json.serialize jsonData
                Client.sendRequest "loginRequest" data
                active_state.[n] <- 1
                
                
        //While login, actions_list has been sent
        //Each actor(user), according to their list will perform these actions
        // 1 - Tweet ; 2 -  Retweet; 3 - Search
                
        | Tweet uid ->
           let mutable user_id = random.Next(1,totalUsers+1)
           let mutable hashtag_1 = hashtag_list.[random.Next(hashtag_list.Length)]
           let mutable hashtag_2 = hashtag_list.[random.Next(hashtag_list.Length)]
           let mutable message = "Blah Blah Blah @"+ string user_id + " Blah Blah Blah Blah #" + string hashtag_1 + " #"+ string hashtag_2
           Client.sendRequest "submitTweetRequest" message
           
        | Retweet uid ->
            Client.sendRequest "submitReTweetRequest" uid
         
        | Search uid ->
            let mutable search_option = random.Next(1,3)
            if search_option = 1 then
                //search_option = 1 -> mymentions search
                Client.sendRequest "myMention"  uid
            else
                //search_option = 2 -> hashtag search
                let mutable to_search = hashtag_list.[random.Next(hashtag_list.Length)]
                Client.sendRequest "tweetWithHashTagSearch" to_search
                
        //When all the list of actions ends, server sends a message after which the user logsout
        | Logout uid ->
            let user_id = int uid
            active_state.[user_id] <- 0
            Client.sendRequest "logoutRequest" uid
            logout_count <- logout_count + 1
            if logout_count = 50 then
                logout_count <- 0
                mailbox.Self <! Done
            
        | Done ->
            cycle_count <- cycle_count+1
            //Terminate
            if cycle_count = 10 then
                mailbox.Context.System.Terminate () |> ignore
            
            else
                mailbox.Self <! BeginProcess
                       
        return! loop()               
    } loop()

let simulator_actor = spawn system "simulator" simulator
simulator_actor <! Start

