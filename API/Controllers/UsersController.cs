using System.ComponentModel;
using System.Security.Claims;
using API.DTOs;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserRepository UserRepository;

    private readonly IMapper Mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        UserRepository = userRepository;
        Mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
        var users = await UserRepository.GetMembersAsync();

        return Ok(users);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
        return await UserRepository.GetMemberAsync(username);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var user = await UserRepository.GetUserByUsername(username);

        if(user == null) return NotFound();

        user = Mapper.Map(memberUpdateDto, user);

        if(await UserRepository.SaveAllAsync()) return NoContent();

        return BadRequest("Failed to update user");
    }
}