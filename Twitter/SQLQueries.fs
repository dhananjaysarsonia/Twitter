module Twitter.SQLQueries

open System.Data.SQLite
open System.Data.SQLite
open System.Data.SQLite
open System.Data.SqlClient


let createUserTableQuery = "CREATE TABLE user(uid TEXT PRIMARY KEY, " +
                           "password TEXT);"
                           
let createFollowerTable = "CREATE TABLE follows(uid TEXT, " +
                          "follows TEXT, FOREIGN KEY(follows) REFERENCES user(uid));"


let createTweetTable = "CREATE TABLE tweet(tweetId TEXT PRIMARY KEY, " +
                          "tweet TEXT, uid TEXT, flag BOOLEAN, origTweet TEXT, FOREIGN KEY(uid) REFERENCES user(uid));"                          
let createMentionTable = "Create TABLE mention( tweetId TEXT," +
                         "mentionId TEXT, FOREIGN KEY(mentionId) REFERENCES user(uid));"

let createHashTagTable = "Create TABLE hashtag_master(id TEXT PRIMARY KEY, "+
                          "hashtag TEXT)"
                          
let createHashTagTweetTable = "Create TABLE hashtag(id TEXT, "+
                              "tweetId TEXT, FOREIGN KEY(tweetId) REFERENCES user(tweetId), FOREIGN KEY(id) REFERENCES hashtag_master(id))"


let feedTable = "Create TABLE feed(uid TEXT, "+
                              "tweetId TEXT, owner TEXT, time DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(uid) REFERENCES user(uid), FOREIGN KEY(tweetId) REFERENCES tweet(tweetId), FOREIGN KEY(owner) REFERENCES user(uid))"


type feed_row = {
    uid : string
    tweetId : string
    date : string
    
}

let dbAddNewUser (userId: string) (password : string) (connection : SQLiteConnection) =
    connection.Open()
    let sql =  "INSERT INTO user (uid, password) VALUES (@uid, @password)" 
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@uid", userId) |> ignore
    command.Parameters.AddWithValue("@password", password) |> ignore

    command.ExecuteNonQuery() |> ignore
    connection.Close()

let dbInsertTweet (tweetId : string) (tweet : string) (uid : string) (flag : int) (owner : string)(connection : SQLiteConnection) =
    connection.Open()
    let sql =  "INSERT INTO tweet (tweetId, tweet, uid) VALUES (@tweetId, @tweet, @uid, @flag, @owner)" 
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@tweet", tweet) |> ignore
    command.Parameters.AddWithValue("@uid", uid) |> ignore
    command.Parameters.AddWithValue("@flag", flag) |> ignore
    command.Parameters.AddWithValue("@owner", owner) |> ignore
    command.ExecuteNonQuery() |> ignore
    connection.Close()
    
let dbInsertFeed (uid : string) (tweetId: string) (owner : string) (connection : SQLiteConnection) =
    connection.Open()
    let sql =  "INSERT INTO feed(uid, tweetId, owner, time) VALUES(@uid, @tweetId, @owner, 'NULL')" 
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@uid", uid) |> ignore
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@owner", owner)
    command.ExecuteNonQuery() |> ignore
    connection.Close()
    
let dbInsertMention(tweetId : string) (uid : string) (connection : SQLiteConnection) =
    connection.Open()
    let sql = "INSERT INTO mention(tweetId, mentionId) VALUES(@tweetId, @mentionId)"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@tweetId", tweetId) |> ignore
    command.Parameters.AddWithValue("@mentionId", uid) |> ignore
    command.ExecuteNonQuery() |> ignore
    connection.Close()
    
let dbInsertHashTag (id : string) (hashtag : string)(connection : SQLiteConnection) =
    connection.Open()
    let sql = "INSERT INTO hashtag_master(id, hashtag) VALUES(@id, @hashtag)"
    let command = new SQLiteCommand(sql, connection)
    command.Parameters.AddWithValue("@id", id) |> ignore
    command.Parameters.AddWithValue("@hashtag", hashtag) |> ignore 
    command.ExecuteNonQuery() |> ignore
    connection.Close()
    



    
//readers

//get hashtag id


//get mention list from tweet


//get follower list for uid




//get hashtags from tweet


//get feed object list for user with uid


//get feed uid
//list feed row, json string
    
    

    
