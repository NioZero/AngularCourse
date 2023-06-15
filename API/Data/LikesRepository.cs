using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikesRepository : ILikesRepository
{
    private readonly DataContext Context;

    public LikesRepository(DataContext context)
    {
        Context = context;    
    }

    public async Task<UserLike> GetUserLike(int sourceUserId, int targetUserId)
    {
        return await Context.Likes.FindAsync(sourceUserId, targetUserId);
    }

    public async Task<PagedList<LikeDto>> GetUserLikes(LikesParam likesParam)
    {
        var users = Context.Users.OrderBy(u => u.UserName).AsQueryable();

        var likes = Context.Likes.AsQueryable();

        if(likesParam.Predicate == "liked")
        {
            likes = likes.Where(l => l.SourceUserId == likesParam.UserId);
            users = likes.Select(l => l.TargetUser);
        }
        else if(likesParam.Predicate == "likedBy")
        {
            likes = likes.Where(l => l.TargetUserId == likesParam.UserId);
            users = likes.Select(l => l.SourceUser);
        }

        var likedUsers = users.Select(u => new LikeDto()
        {
           UserName = u.UserName,
           KnownAs = u.KnownAs,
           Age = u.DateOfBirth.CalculateAge(),
           PhotoUrl = u.Photos.FirstOrDefault(x => x.IsMain).Url,
           City = u.City,
           Id = u.Id
        });

        return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParam.pageNumber, likesParam.PageSize);
    }

    public async Task<AppUser> GetUserWithLikes(int userId)
    {
        return await Context.Users.Include(e => e.LikedUsers).FirstOrDefaultAsync(u => u.Id == userId);
    }
}