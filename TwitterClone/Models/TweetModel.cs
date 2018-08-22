using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TwitterClone.Models
{
    public class TweetModel
    {
        public int TweetId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public bool IsMyTweet { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Created { get; set; }
    }
}