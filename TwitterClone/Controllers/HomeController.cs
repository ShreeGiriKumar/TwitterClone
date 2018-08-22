using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TwitterClone.EF;
using TwitterClone.Models;

namespace TwitterClone.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        TwitterCloneEntities _twitterCloneEntity = new TwitterCloneEntities();
        public ActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public JsonResult TweetFollowCount()
        {
            try
            {
                var tweetCount = _twitterCloneEntity.Tweets.Where(x => x.user_id == User.Identity.Name).Count();

                var following = (from p in _twitterCloneEntity.People
                                 join t in _twitterCloneEntity.Followings on p.user_id equals t.user_id
                                 where p.user_id == User.Identity.Name &&
                                 _twitterCloneEntity.People.Where(x => x.user_id == t.following_id).Any(t => t.active)
                                 select t).Count();

                var followers = (from p in _twitterCloneEntity.People
                                 join t in _twitterCloneEntity.Followings on p.user_id equals t.following_id
                                 where p.user_id == User.Identity.Name &&
                                 _twitterCloneEntity.People.Where(x => x.user_id == t.user_id).Any(t => t.active)
                                 select t).Count();

                var result = new { tweetCount = tweetCount , following = following, followers = followers };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult AllTweets()
        {
            try
            {
                var followingIds = (from p in _twitterCloneEntity.People
                                   join t in _twitterCloneEntity.Followings on p.user_id equals t.user_id
                                   where p.user_id == User.Identity.Name &&
                                   _twitterCloneEntity.People.Where(x => x.user_id == t.following_id).Any(t => t.active)
                                   select t.following_id).ToList<string>();

                var followerIds = (from p in _twitterCloneEntity.People
                                  join t in _twitterCloneEntity.Followings on p.user_id equals t.following_id
                                  where p.user_id == User.Identity.Name &&
                                  _twitterCloneEntity.People.Where(x => x.user_id == t.user_id).Any(t => t.active)
                                  select t.user_id).ToList<string>();

                followingIds.Add(User.Identity.Name);

                var tweetUserIds = followingIds.Union(followerIds).Distinct();

                List<TweetModel> tweets = _twitterCloneEntity.Tweets
                    .Where(x => tweetUserIds.Contains(x.user_id))
                    .Select(x => new TweetModel()
                    {
                        UserId = x.user_id,
                        IsMyTweet = x.user_id == User.Identity.Name,
                        Message = x.message,
                        TweetId = x.tweet_id,
                        CreatedDate = x.created
                    }).ToList<TweetModel>();

                tweets.ForEach(x => {
                    x.Created = x.CreatedDate.Date == DateTime.Today.Date ? x.CreatedDate.ToString("HH:mm") : x.CreatedDate.ToString("dd-MMM");
                });

                return Json(tweets,JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PostTweet(string tweet)
        {
            try
            {
                _twitterCloneEntity.Tweets.Add(new Tweet()
                {
                    created = DateTime.Now,
                    message = tweet,
                    user_id = User.Identity.Name
                });

                _twitterCloneEntity.SaveChanges();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetTweet(int tweetId)
        {
            try
            {
                string message = _twitterCloneEntity.Tweets.Where(x => x.tweet_id == tweetId).Select(x => x.message).FirstOrDefault();
                int tweet_Id = _twitterCloneEntity.Tweets.Where(x => x.tweet_id == tweetId).Select(x => x.tweet_id).FirstOrDefault();

                var result = new { Message = message, TweetId = tweetId};

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult EditTweet(string tweet, int tweetId)
        {
            try
            {
                Tweet oldTweet = _twitterCloneEntity.Tweets.Where(x => x.tweet_id == tweetId).Select(x => x).FirstOrDefault();
                oldTweet.message = tweet;                
                _twitterCloneEntity.SaveChanges();

                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult DeleteTweet(int tweetId)
        {
            try
            {
                Tweet tweetToDelete = _twitterCloneEntity.Tweets.Where(x => x.tweet_id == tweetId).Select(x => x).FirstOrDefault();
                _twitterCloneEntity.Tweets.Remove(tweetToDelete);

                _twitterCloneEntity.SaveChanges();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult SearchPerson(string userId)
        {
            try
            {
                bool isAvailable = false;
                if (_twitterCloneEntity.People.Any(x => x.user_id == userId))
                    isAvailable = true;
                
                return Json(isAvailable, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public JsonResult FollowPerson(string followingUserId)
        {
            try
            {
                Following following = new Following() { user_id = User.Identity.Name, following_id = followingUserId };
                _twitterCloneEntity.Followings.Add(following);

                _twitterCloneEntity.SaveChanges();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
        }
    }
}