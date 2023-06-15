using System.Linq.Expressions;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LikesController : BaseApiController
{
    private readonly IUserRepository UserRepository;

    private readonly ILikesRepository LikesRepository;

    private readonly IMapper Mapper;

    public LikesController(IUserRepository userRepository, ILikesRepository likesRepository, IMapper mapper)
    {
        UserRepository = userRepository;
        LikesRepository = likesRepository;
        Mapper = mapper;
    }

    [HttpPost("{username}")]
    public async Task<ActionResult> AddLike(string username)
    {
        var sourceUserId = User.GetUserID();
        var likedUser = await UserRepository.GetUserByUsername(username);
        var sourceUser = await LikesRepository.GetUserWithLikes(sourceUserId);

        if(likedUser == null) return NotFound();

        if(sourceUser.UserName == username) return BadRequest("You cannot like yourself");

        var userLike = await LikesRepository.GetUserLike(sourceUserId, likedUser.Id);

        if(userLike != null) return BadRequest("You already like this user");

        userLike = new UserLike()
        {
            SourceUserId = sourceUserId,
            TargetUserId = likedUser.Id
        };

        sourceUser.LikedUsers.Add(userLike);

        if(await UserRepository.SaveAllAsync()) return Ok();

        return BadRequest("Failed to like user");
    } 

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLikes([FromQuery] LikesParam likesParam)
    {
        likesParam.UserId = User.GetUserID();

        var users = await LikesRepository.GetUserLikes(likesParam);

        Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));

        return Ok(users);
    }

}