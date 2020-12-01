module Twitter.SQLQueries

open System.Data.SQLite
open System.Data.SQLite
open System.Data.SQLite
open System.Data.SQLite
open System.Security.Cryptography
open Twitter.DataTypes.Response


let createUserTableQuery = "CREATE TABLE user(uid TEXT PRIMARY KEY, " +
                           "password TEXT);"
                           
let createFollowerTable = "CREATE TABLE follows(uid TEXT, " +
                          "follows TEXT, FOREIGN KEY(follows) REFERENCES user(uid));"

let createTweetTable = "CREATE TABLE tweet(tweetId TEXT PRIMARY KEY, " +
                          "tweet TEXT, uid TEXT, flag BOOLEAN, owner TEXT, FOREIGN KEY(uid) REFERENCES user(uid));"                          
let createMentionTable = "Create TABLE mention( tweetId TEXT," +
                         "tweet TEXT,mentionId TEXT, uid TEXT, FOREIGN KEY(mentionId) REFERENCES user(uid));"

let createHashTagTable = "Create TABLE hashtag_master(hashtag TEXT PRIMARY KEY);"
                          
let createHashTagTweetTable = "Create TABLE hashtag(hashtag TEXT, "+
                              "uid TEXT,tweetId TEXT, tweet TEXT,FOREIGN KEY(tweetId) REFERENCES tweet(tweetId), FOREIGN KEY(hashtag) REFERENCES hashtag_master(hashtag))"


let feedTable = "Create TABLE feed(uid TEXT, "+
                              "tweetId TEXT, owner TEXT, flag BOOLEAN, origOwner TEXT, timestamp DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(uid) REFERENCES user(uid), FOREIGN KEY(tweetId) REFERENCES tweet(tweetId), FOREIGN KEY(owner) REFERENCES user(uid))"



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
    let sql =  "INSERT INTO tweet (tweetId, tweet, uid) VALUES (@tweetId, @tweet, @uid, @flag, @owner)" 
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
    let sql = "INSERT INTO hashtag_master(id, hashtag) VALUES(@id, @hashtag)"
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
    
//
//type userInfo =
//    {
//        userId : string
//    }
//    
//    
//type userList = {
//    rows : list<userInfo>
//}
//    
//type tweet = {
//    tweetId : string
//    tweet : string
//    uid : string //owner
//    flag : bool //true means it's a retweet
//    origTweetId : string //original owner if retweeted 
//}
//
//type tweetList = {
//    tweets : list<tweet>
//}
//
//type feed_row = {
//    uid : string
//    tweet : tweet
//    date : string
//}
//
//type feeds ={
//    uid : string
//    rows : list<feed_row> 
//}
//
//type mention = {
//    userId : string
//    tweetId : string
//    tweet : string
//    tweetOwner : string
//}
//type mentions = {
//    userId : string
//    rows : list<mention>
//}
//
//type hashtag = {
//    hashtag : string
//}
//
//type hashtags = {
//    data : list<string>
//}
//type hashtagsInTweet = {
//    tweetId : string
//    rows : list<hashtag>
//}
//
//type tweetsInHashTag = {
//    hashtag : string
//    hashtagId : string
//    rows : list<tweet>
//}
//
//type mentionInTweets = {
//    userId : string
//    rows : list<tweet>
//}

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
            tweetId = reader.["uid"].ToString()
            tweet = reader.["tweet"].ToString()
        }
        
        rowsData <- rowsData @ [row]
    connection.Close()
    let mentionData : mentions = {
        userId = uid
        rows = rowsData
        
    }
    mentionData
   
//   let data : mentions = {
//       userId = userId
//       rows = rows
//   }
        
    
//let createMentionTable = "Create TABLE mention( tweetId TEXT," +
//                         "tweet TEXT,mentionId TEXT, uid TEXT, FOREIGN KEY(mentionId) REFERENCES user(uid));"    
    

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
   




    
