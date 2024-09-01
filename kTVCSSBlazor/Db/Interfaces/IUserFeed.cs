using kTVCSSBlazor.Db.Models.UserFeed;

namespace kTVCSSBlazor.Db.Interfaces
{
    public interface IUserFeed
    {
        void Add(Post post);
        List<Post> GetPosts();
        Post GetPost(int postId);
        void AddComment(Comment comment, Post post);
        void Like(int postId, int userId, bool isLike);
        //void GetLikeDislikeCount(int postId, out int likeCount, out int dislikeCount);
        void RemovePost(int postId);
    }
}
