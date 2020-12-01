
module Twitter.DataTypes
open System.Data.SQLite
open FSharp.Json

module Response =
    module types = 
    //response strings
        [<Literal>]
        let userInfoResponse = "userInfoResponse"
        [<Literal>]
        let feedResponse = "feedResponse"
        [<Literal>]
        let tweetDetailResponse = "tweetDetailResponse"
        [<Literal>]
        let followersResponse = "followersResponse"
        [<Literal>]
        let mentionResponse = "mentionResponse"
        [<Literal>]
        let allHashTagSearchResponse = "allHashTagResponse"
        [<Literal>]
        let hashTagTweetsResponse = "hashTagTweetResponse"
        [<Literal>]
        let registerResponse = "registerResponse"
        [<Literal>]
        let loginResponse = "loginResponse"
        [<Literal>]
        let logoutResponse = "logoutResponse"
        
        [<Literal>]
        let sendTweetResponse = "tweetResponse"
        [<Literal>]
        let sendTweetInFeed = "tweetFeedInsertResponse"
    
    
    
    type masterData = {
        option : string
        data : string
    }
    
    type userInfo =
        {
            userId : string
        }
        
        
    type userList = {
        rows : list<userInfo>
    }
        
    type tweet = {
        tweetId : string
        tweet : string
        uid : string //owner
        flag : bool //true means it's a retweet
        origTweetId : string //original owner if retweeted 
    }
    
    type tweetList = {
        tweets : list<tweet>
    }
    
    type feed_row = {
        uid : string
        tweet : tweet
        date : string
    }
    
    type feeds ={
        uid : string
        rows : list<feed_row> 
    }
    
    type mention = {
        userId : string
        tweetId : string
        tweet : string
        tweetOwner : string
    }
    type mentions = {
        userId : string
        rows : list<mention>
    }
    
    type hashtag = {
        hashtag : string
    }
    
    type hashtags = {
        data : list<string>
    }
    type hashtagsInTweet = {
        tweetId : string
        rows : list<hashtag>
    }
    
    type tweetsInHashTag = {
        hashtag : string
        hashtagId : string
        rows : list<tweet>
    }
    
    type mentionInTweets = {
        userId : string
        rows : list<tweet>
    }


module Request =
    module types =
        [<Literal>]
        let userInfoRequest = "userInfoRequest"
        [<Literal>]
        let submitTweetRequest = "submitTweetRequest"
        
        [<Literal>]
        let feedRequest = "feedRequest"
        [<Literal>]
        let tweetDetailRequest = "tweetDetailRequest"
        [<Literal>]
        let followersRequest = "followersRequest"
        [<Literal>]
        let mentionRequest = "mentionRequest"
        [<Literal>]
        let allHashTagSearchRequest = "allHashTagRequest"
        [<Literal>]
        let hashTagTweetRequest = "hashTagTweetRequest"
        [<Literal>]
        let registerRequest : string = "registerRequest"
        [<Literal>]
        let loginRequest = "loginRequest"
        [<Literal>]
        let logoutRequest = "logoutRequest"
    
    
    
    
    type masterData = {
        option : string
        data : string
    }
    
    type registerRequest = {
        uid : string
        password : string
    }
    type loginRequest = {
        uid : string
        password : string
    }
    
    type followRequest = {
        uid : string
        follow_id : string
    }
    
    type tweetSubmitRequest = {
        uid : string
        tweetId : string //"submit it empty initially"
        isRetweet : bool
        tweet : string
        mentions : string[]
        hashtags : string[]
        origOwner : string
    }
    
    type retweetRequest = {
        uid : int
        tweet_id : string
    }
    
    type searchHashTagRequest = {
       uid : string
       to_search : string
    }
    type searchMyMentionRequest = {
        uid : string
    }
    
    type logoutRequest = {
        uid : string
    }
    
    type feedRequest = {
        uid : string
    }
