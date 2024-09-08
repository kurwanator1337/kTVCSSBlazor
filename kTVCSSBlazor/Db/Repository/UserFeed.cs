using Dapper;
using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.UserFeed;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using VkNet.Model;

namespace kTVCSSBlazor.Db.Repository
{
    public class UserFeed(IConfiguration configuration, ILogger logger) : Context(configuration, logger), IUserFeed
    {
        public void Add(Models.UserFeed.Post post)
        {
            EnsureConnected();

            var parameters = new
            {
                AuthorName = post.AuthorName,
                AuthorId = post.AuthorId,
                AuthorAvatar = post.AuthorAvatar,
                Content = post.Content,
                MediaUrl = post.MediaUrl,
                MediaType = post.MediaType,
                NewComment = post.NewComment,
                IsVip = post.IsVip,
                IsAdmin = post.IsAdmin
            };

            Db.Execute("AddPost", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }

        public Models.UserFeed.Post GetPost(int postId)
        {
            EnsureConnected();

            var post = Db.QueryFirstOrDefault<Models.UserFeed.Post>($"SELECT * FROM Posts WHERE postId = {postId}");
            var coms = Db.Query<Models.UserFeed.Comment>($"SELECT * FROM Comments WHERE PostId = {post.PostId}");
            if (coms.Any())
            {
                post.Comments.AddRange(coms);
            }
            post.Likes = Db.Query<Like>($"SELECT * FROM PostsLikes WHERE PostId = {post.PostId}").ToList();

            return post;
        }

        public List<Models.UserFeed.Post> GetPosts()
        {
            EnsureConnected();

            var posts = Db.Query<Models.UserFeed.Post>("SELECT TOP(30) * FROM Posts ORDER BY PostDate DESC");

            foreach (var post in posts)
            {
                var coms = Db.Query<Models.UserFeed.Comment>($"SELECT * FROM Comments WHERE PostId = {post.PostId}");
                if (coms.Any())
                {
                    post.Comments.AddRange(coms);
                }
                post.Likes = Db.Query<Like>($"SELECT * FROM PostsLikes WHERE PostId = {post.PostId}").ToList();
            }

            return posts.ToList();
        }

        public void AddComment(Models.UserFeed.Comment comment, Models.UserFeed.Post post)
        {
            EnsureConnected();

            var parameters = new
            {
                PostId = comment.PostId,
                AuthorName = comment.AuthorName,
                AuthorAvatar = comment.AuthorAvatar,
                AuthorId = comment.AuthorId,
                Content = comment.Content,
                IsVip = comment.IsVip,
                IsAdmin = comment.IsAdmin
            };

            Db.Execute("AddComment", parameters, commandType: System.Data.CommandType.StoredProcedure);
            
            foreach (var c in post.Comments)
            {
                if (c.AuthorId != comment.AuthorId)
                {
                    try
                    {
                        DynamicParameters d = new DynamicParameters();
                        d.Add("ID", c.AuthorId);
                        d.Add("TEXT", $"{comment.AuthorName} оставил комментарий к Вашему посту!");

                        Db.Execute($"AddAlert", d, commandType: CommandType.StoredProcedure);
                    }
                    catch (Exception)
                    {
                        // ?
                    }
                }
            }
        }

        public void Like(int postId, int userId, bool isLike)
        {
            EnsureConnected();

            var like = Db.QuerySingleOrDefault<Like>(
                   "SELECT * FROM PostsLikes WHERE PostId = @PostId AND UserId = @UserId",
                   new { PostId = postId, UserId = userId });

            if (like == null)
            {
                // Лайк или дизлайк еще не оставлен
                var sql = "INSERT INTO PostsLikes (PostId, UserId, IsLike) VALUES (@PostId, @UserId, @IsLike)";
                Db.Execute(sql, new { PostId = postId, UserId = userId, IsLike = isLike });
            }
            else
            {
                // Лайк или дизлайк уже существует, обновим его
                var sql = "UPDATE PostsLikes SET IsLike = @IsLike WHERE LikeId = @LikeId";
                Db.Execute(sql, new { LikeId = like.LikeId, IsLike = isLike });
            }
        }

        public void RemovePost(int postId)
        {
            EnsureConnected();

            Db.Execute($"DELETE FROM Posts WHERE PostId = {postId}");
        }

        public int GetDislikeCount(int postId)
        {
            EnsureConnected();

            var sql = "SELECT SUM(CASE WHEN IsLike = 1 THEN 1 ELSE 0 END) AS LikeCount, " +
                         "SUM(CASE WHEN IsLike = 0 THEN 1 ELSE 0 END) AS DislikeCount " +
                         "FROM Likes WHERE PostId = @PostId";

            var result = Db.QuerySingle(sql, new { PostId = postId });

            return int.Parse(result.DislikeCount.ToString());
        }
    }
}
