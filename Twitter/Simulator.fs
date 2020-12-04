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
open Client
open Twitter.DataTypes.Request

//Commandline input for .fsx file
//let args = fsi.CommandLineArgs
//let totalUsers = System.Int32.Parse(args.[1])

let totalUsers = 100
let mutable number_login = 50 * totalUsers / 100
    
let system = System.create "system" (Configuration.defaultConfig())

type Message =
    | Start
    | BeginSimulation
    | LogoutDone of string
    | Done

let containsNumber number list = List.exists (fun elem -> elem = number) list 
let random = new System.Random()
let active_state = Array.zeroCreate totalUsers
let activity_count = Array.zeroCreate totalUsers
let mutable logout_count = 0
let mutable cycle_count = 0
let mutable activity_daycount =0

let timer = new System.Diagnostics.Stopwatch()

let mutable actorMap = new Dictionary<string, IActorRef>()


// Create a list of hashtags
let hashtag_list = ["#1"; "#2"; "#3"; "#4"; "#5"; "#6"; "#7"; "#8"; "#9"; "#10"]

let requestBuilder option data =
    let req : DataTypes.Request.masterData = {
        option = option
        data = data
    }
    req

let simulator(mailbox : Actor<_>) =
    let rec loop() = actor{
        let! message = mailbox.Receive()
        match message with
        | Start ->
            //Register
            let mutable data = string 1
            for i in 1 .. totalUsers do
                let actorRef = spawn system (string <| i) clientActor
                actorMap.Add(string <| i, actorRef)
                let regRequest : DataTypes.Request.registerRequest = {
                    uid = (string <| i)
                    password = ""
                }
                actorRef <! requestBuilder DataTypes.Request.types.registerRequest (Json.serialize regRequest)
            //All users are registered
            
            //Follow
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
                let actorRef = actorMap.[string <| i]
                actorRef <! requestBuilder DataTypes.Request.types.followRequest data
            mailbox.Self <! BeginSimulation 
            //Network is built
                
            //Simulation Begins
        | BeginSimulation ->
            
            timer.Start()
            let mutable n = 0
            let mutable data = string n
            for i in 1 .. number_login do    
                while active_state.[n] = 1 do
                    n <- random.Next(1,totalUsers+1)
                //To create a list of actions
                let mutable action_list = []
                for i in 1 .. activity_count.[n] do
                    let mutable temp = random.Next(1,4)
                    action_list <- action_list @ [temp]
                let mutable listofactions = []
                for i in action_list do
                    if i = 1 then
                        
                        //Tweet
                        let mutable user_id = random.Next(1,totalUsers+1)
                        let mutable hashtag_1 = hashtag_list.[random.Next(hashtag_list.Length)]
                        let mutable hashtag_2 = hashtag_list.[random.Next(hashtag_list.Length)]
                        let mutable message = "Blah Blah Blah @"+ string user_id + " Blah Blah Blah Blah #" + string hashtag_1 + " #"+ string hashtag_2
                        let mutable temp = requestBuilder DataTypes.Request.types.submitTweetRequest message
                        listofactions <- listofactions @ [temp]
                        
                    elif i = 2 then
                        
                        //Retweet
                        let mutable temp = requestBuilder DataTypes.Request.types.submitReTweetRequest ""
                        listofactions <- listofactions @ [temp]
                        
                    else
                        
                        // Search 
                        let mutable search_option = random.Next(1,3)
                        if search_option = 1 then
                            let mutable temp = requestBuilder DataTypes.Request.types.mentionRequest ""
                            listofactions <- listofactions @ [temp]
                            
                        else
                            //search_option = 2 -> hashtag search
                            let mutable to_search = hashtag_list.[random.Next(hashtag_list.Length)]
                            let mutable temp = requestBuilder DataTypes.Request.types.hashTagTweetRequest to_search
                            
                            listofactions <- listofactions @ [temp]
                            
                 
                let jsonData :  DataTypes.Request.loginRequest = { uid = string i ; password = " " ; actions_list = listofactions}
                let data = Json.serialize jsonData
                
                let actorRef = actorMap.[string <| i]
                actorRef <! requestBuilder DataTypes.Request.types.loginRequest data
                active_state.[n] <- 1
                
        //when a user finishes its activities it prompts the simulator to logout
        | LogoutDone uid ->
            let user_id = int uid
            activity_daycount <- activity_daycount + activity_count.[user_id]
            active_state.[user_id] <- 0
            let actorRef = actorMap.[uid]
            actorRef <! requestBuilder DataTypes.Request.types.logoutRequest uid
            
            //I will give metric for each actor here
            
            logout_count <- logout_count + 1
            if logout_count = number_login then
                logout_count <- 0
                //calculate total metric here and print 
                mailbox.Self <! Done
            
        | Done ->
            cycle_count <- cycle_count+1
            //Terminate
            timer.Stop()
            printfn "Day %i ends." cycle_count
            printfn "Time taken to process %i requests is %f milliseconds." activity_daycount timer.Elapsed.TotalMilliseconds
            if cycle_count = 5 then
                printf "Simulation Complete"
                mailbox.Context.System.Terminate () |> ignore
            
            else
                mailbox.Self <! BeginSimulation
                       
        return! loop()               
    } loop()

let simulator_actor = spawn system "simulator" simulator
simulator_actor <! Start

