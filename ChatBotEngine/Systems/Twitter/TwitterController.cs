﻿using System;
using TweetSharp;
using TweetSharp.Model;

namespace HD
{
  public class TwitterController
  {
    public static bool SendTweet(
      string text,
      bool isForLiveThread)
    {
      TwitterService service = new TwitterService(
        BotSettings.twitter.consumerKey,
        BotSettings.twitter.consumerSecret);
      service.AuthenticateWith(
        BotSettings.twitter.accessToken,
        BotSettings.twitter.accessTokenSecret);

      SendTweetOptions options = new SendTweetOptions();
      options.Status = text;
      if (isForLiveThread)
      {
        options.InReplyToStatusId = 872319273645035521;
      }

      TwitterStatus status = null;
      try
      {
        status = service.SendTweet(options);
      }
      catch { }

      if (status == null)
      {
        return false;
      }
      else
      {
        return true;
      }
    }
  }
}
