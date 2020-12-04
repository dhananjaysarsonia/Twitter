module Twitter.SQLQueries

open System.Data.SQLite
open System.Data.SQLite
open System.Data.SQLite
open System.Data.SQLite
open System.Security.Cryptography
open Twitter.DataTypes.Response


let createUserTableQuery = "CREATE TABLE IF NOT EXISTS user(uid VARCHAR(128) PRIMARY KEY, password VARCHAR(128));"
                           
                           
                           
let createFollowerTable = "CREATE TABLE follows(uid TEXT, " +
                          "follows TEXT);"

let createTweetTable = "CREATE TABLE tweet(tweetId TEXT PRIMARY KEY, " +
                          "tweet TEXT, uid TEXT, flag BOOLEAN, owner TEXT);"                          
let createMentionTable = "Create TABLE mention( tweetId TEXT," +
                         "tweet TEXT,mentionId TEXT, uid TEXT);"
                             
let createHashTagTable = "Create TABLE hashtag_master(hashtag TEXT PRIMARY KEY);"
                          
let createHashTagTweetTable = "Create TABLE hashtag(hashtag TEXT, "+
                              "uid TEXT,tweetId TEXT, tweet TEXT)"


let feedTable = "Create TABLE feed(uid TEXT, "+
                              "tweetId TEXT, tweet TEXT, owner TEXT, flag BOOLEAN, origOwner TEXT, timestamp DEFAULT CURRENT_TIMESTAMP)"



let dbAddNewUser (userId: string) (password : string) (connection : SQLiteConnection) =
    connection.Open()
    let sql =  "INSERT INTO user (uid, password) VALUES (@uid, @password)" 
    let command = new SQLiteCommand(sql, connection)
    
    command.Parameters.AddWithValue("@uid", userId) |> ignore
    command.Parameters.AddWithValue("@password", password) |> ignore

    command.ExecuteNonQuery() |> ignore
    connection.Close()

let dbInsertTweet (tweetId : string) (tweet : string) (uid : string) (flag : bool) (owner : string) (connection : SQLiteConnection) =
    //connection.Open()
    let sql =  "INSERT INTO tweet (tweetId, tweet, uid, flag, owner) VALUES (@tweetId, @tweet, @uid, @flag, @owner)" 
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@tweet", tweet) |> ignore
    command.Parameters.AddWithValue("@uid", uid) |> ignore
    command.Parameters.AddWithValue("@flag", flag) |> ignore
    command.Parameters.AddWithValue("@owner", owner) |> ignore
    command.ExecuteNonQuery() |> ignore
    //connection.Close()
    
let dbInsertFeed (uid : string) (tweetId: string) (tweet : string) (owner : string) (flag : bool) (origOwner : string)(connection : SQLiteConnection) =
//    connection.Open()
    let sql =  "INSERT INTO feed(uid, tweetId, tweet, owner, flag, origOwner, time) VALUES(@uid, @tweetId, @tweet, @owner, 'NULL')" 
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@uid", uid) |> ignore
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@tweet", tweet) |> ignore
    command.Parameters.AddWithValue("@owner", owner) |> ignore
    command.Parameters.AddWithValue("@flag", flag) |> ignore
    command.Parameters.AddWithValue("@origOwner", origOwner) |> ignore

    command.ExecuteNonQuery() |> ignore
    //connection.Close()
    
let dbInsertMention(tweetId : string) (tweet : string) (mentionId : string) (uid : string) (connection : SQLiteConnection) =
   // connection.Open()
    let sql = "INSERT INTO mention(tweetId, tweet, mentionId, uid) VALUES(@tweetId, @tweet, @mentionId, @uid)"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@tweet", tweet) |> ignore
    command.Parameters.AddWithValue("@mentionId", mentionId) |> ignore
    command.Parameters.AddWithValue("@uid", uid) |> ignore //owner of the tweet
    command.ExecuteNonQuery() |> ignore
  //  connection.Close()
    
let dbInsertHashTag (hashtag : string)(connection : SQLiteConnection) =
   // connection.Open()
    let sql = "INSERT INTO hashtag_master(hashtag) VALUES(@hashtag)"
    let command = new SQLiteCommand(sql, connection)    
    command.Parameters.AddWithValue("@hashtag", hashtag) |> ignore 
    command.ExecuteNonQuery() |> ignore
  //  connection.Close()
    
let dbInsertHashTagForTweet (hashtag : string) (uid : string) (tweetId : string) (tweet : string)(connection : SQLiteConnection) =
    connection.Open()
    let sql = "INSERT INTO hashtag(hashtag, uid, tweetId, tweet) VALUES(@id, @hashtag)"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@hashtag", hashtag) |> ignore
    command.Parameters.AddWithValue("@uid", uid) |> ignore
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@tweet", tweet) |> ignore
    command.ExecuteNonQuery() |> ignore
    connection.Close()
    
    
let dbInsertFollow (fromId : string) (toId : string) (connection : SQLiteConnection) =
    connection.Open()
    let sql = "INSERT INTO follows(uid, follows) VALUES( @fromId, @toId)"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@fromId", fromId) |> ignore
    command.Parameters.AddWithValue("@toId", toId) |> ignore
    command.ExecuteNonQuery () |> ignore
    connection.Close ()
    


let dbGetTweetFotTweetIdWithConnectionOpen (tweetId : string) (connection : SQLiteConnection) =
    let mutable tweets : list<tweet> = []
    let sql = "SELECT * FROM tweet where tweetId = @tweetId"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    let reader = command.ExecuteReader()
    while reader.Read() do
        let data : tweet = {
            tweetId = reader.["tweetId"].ToString()
            uid = reader.["uid"].ToString()
            tweet = reader.["tweet"].ToString()
            flag = System.Convert.ToBoolean(reader.["flag"])
            origTweetId = reader.["owner"].ToString()
        }
        tweets <- tweets @ [data]
    let tweetList : tweetList = {
        tweets = tweets
    }
    tweetList
    
    
let dbGetTweetFotTweetIdWithConnectionNotOpen (tweetId : string) (connection : SQLiteConnection) =
    connection.Open()
    let mutable tweets : list<tweet> = []
    let sql = "SELECT * FROM tweet where tweetId = @tweetId"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    let reader = command.ExecuteReader()
    while reader.Read() do
        let data : tweet = {
            tweetId = reader.["tweetId"].ToString()
            uid = reader.["uid"].ToString()
            tweet = reader.["tweet"].ToString()
            flag = System.Convert.ToBoolean(reader.["flag"])
            origTweetId = reader.["owner"].ToString()
        }
        tweets <- tweets @ [data]
    connection.Close()
    let tweetList : tweetList = {
        tweets = tweets
    }
    tweetList
    
    
//get feed for user
let dbGetFeed (uid : string) (connection : SQLiteConnection) =
    connection.Open()
    let mutable feedRows : list<feed_row> = []
    let sql = "SELECT * FROM feed WHERE uid = @uid ORDER BY timestamp DESC LIMIT 100"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@uid", uid) |> ignore
    let reader = command.ExecuteReader()
    while reader.Read() do
        let feedRow : feed_row = {
            uid = reader.["owner"].ToString()
            date = reader.["timestamp"].ToString()
            tweet = {
                tweetId = reader.["tweetId"].ToString()
                uid = reader.["owner"].ToString()
                tweet = reader.["tweet"].ToString()
                flag = System.Convert.ToBoolean(reader.["flag"])
                origTweetId = reader.["origOwner"].ToString()
            }
        }
            
        feedRows <- feedRows @ [feedRow]
    connection.Close()
    let feed : feeds = {
        uid = uid
        rows = feedRows
    }
    feed




//get mentions of a user
let dbGetMentionsOfUser (uid : string) (connection : SQLiteConnection) =
    let sql = "SELECT * FROM mention WHERE mentionId = @mentionId"
    let command = new SQLiteCommand(sql, connection)
    connection.Open()
    let mutable rowsData : list<mention> = []
    command.Parameters.AddWithValue("@mentionId", uid) |> ignore
    let reader = command.ExecuteReader ()
    while reader.Read() do
        let row : mention = {
            userId = reader.["mentionId"].ToString()
            tweetOwner = reader.["uid"].ToString()
            tweetId = reader.["tweetId"].ToString()
            tweet = reader.["tweet"].ToString()
        }
        
        rowsData <- rowsData @ [row]
    connection.Close()
    let mentionData : mentions = {
        userId = uid
        rows = rowsData
        
    }
    mentionData

    

//get all hashtags
let dbGetAllHashTag (connection : SQLiteConnection) =
    let sql = "SELECT * from hashtag_master"
    connection.Open ()
    let mutable data : list<string> = []
    let command = new SQLiteCommand(sql, connection)
    let reader = command.ExecuteReader()
    while reader.Read() do
        data <- data @ [reader.["hashtag"].ToString()]
    
    let hashlist : hashtags = {
        data = data
    }
    connection.Close()
    
    hashlist
    

    

//get tweets with hashtags

let dbGetTweetWithTag(hashtag : string) (connection : SQLiteConnection) =
    connection.Open()
    let sql = "SELECT * FROM hashtag WHERE hashtag = @hashtag"
    let mutable data : list<tweet> = []
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@hashtag", hashtag) |> ignore
    let reader = command.ExecuteReader()
    while reader.Read() do
        let rowTweet : tweet = {
            tweetId = reader.["tweetId"].ToString()
            tweet = reader.["tweet"].ToString()
            uid = reader.["uid"].ToString()
            flag = false
            origTweetId = ""
            
        }
        data <- data @ [rowTweet]
    connection.Close()
    data
    

//search all users


//get followers of user
let dbGetUserFollowers(uid : string) ( connection : SQLiteConnection) =
    connection.Open()
    // sql = "Sele"
    let sql = "SELECT * FROM follows where follows = @uid"
    let command = new SQLiteCommand(sql, connection)
    let mutable users : list<userInfo> = []
    command.Parameters.AddWithValue("@uid",uid) |> ignore 
    let reader = command.ExecuteReader()
    while reader.Read() do
        let row : userInfo = {
            userId = reader.["uid"].ToString()
        }
        users <- users @ [row]
        
    let userList : userList = {
        rows = users
    }
    
    connection.Close()
    userList
    
let dbGetAllUsers (connection : SQLiteConnection) =
    connection.Open ()
    let sql = "SELECT * from user"
    let command = new SQLiteCommand(sql, connection)
    let mutable users : list<userInfo> = []
    let reader = command.ExecuteReader()
    while reader.Read() do
        let row : userInfo = {
            userId = reader.["uid"].ToString()
        }
        users <- users @ [row]
    
    connection.Close()
    let userList : userList = {
        rows = users
    }
    connection.Close()
    userList
   




    
