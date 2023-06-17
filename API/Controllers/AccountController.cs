using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly UserManager<AppUser> UserManager;

    private readonly IMapper Mapper;

    private readonly ITokenService TokenService;

    public AccountController(UserManager<AppUser> userManager, IMapper mapper, ITokenService tokenService)
    {
        UserManager = userManager;
        Mapper = mapper;
        TokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if(await UserExists(registerDto.Username)) return BadRequest("Username is taken");


        var user = Mapper.Map<AppUser>(registerDto);

        user.UserName = registerDto.Username.ToLower();

        var result = await UserManager.CreateAsync(user, registerDto.Password);

        if(!result.Succeeded) return BadRequest(result.Errors);

        var roleResult = await UserManager.AddToRoleAsync(user, "Member");

        if(!roleResult.Succeeded) return BadRequest(result.Errors);

        return Ok(new UserDto
        {
            Username = user.UserName,
            Token = await TokenService.CreateToken(user),
            KnownAs = user.KnownAs,
            Gender = user.Gender
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await UserManager.Users
                    .Include(m => m.Photos)
                    .SingleOrDefaultAsync(u => u.UserName == loginDto.Username);

        if(user == null) return Unauthorized("invalid username");

        var result = await UserManager.CheckPasswordAsync(user, loginDto.Password);

        if(!result) return Unauthorized("Invalid password");        

        return Ok(new UserDto
        {
            Username = user.UserName,
            Token = await TokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
            KnownAs = user.KnownAs,
            Gender = user.Gender
        });
    }

    private async Task<bool> UserExists(string username)
    {
        return await UserManager.Users.AnyAsync(u => u.UserName == username.ToLower());
    }
}