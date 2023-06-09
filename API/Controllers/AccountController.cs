using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly DataContext Context;

    private readonly ITokenService TokenService;

    public AccountController(DataContext context, ITokenService tokenService)
    {
        Context = context;
        TokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if(await UserExists(registerDto.Username)) return BadRequest("Username is taken");

        using var hmac = new HMACSHA512();

        var user = new AppUser()
        {
            UserName = registerDto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };

        Context.Users.Add(user);

        await Context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Username = user.UserName,
            Token = TokenService.CreateToken(user)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await Context.Users
                    .Include(m => m.Photos)
                    .SingleOrDefaultAsync(u => u.UserName == loginDto.Username);

        if(user == null) return Unauthorized("invalid username");

        using var hmac = new HMACSHA512(user.PasswordSalt);

        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        for(int i=0;i<computedHash.Length; i++)
        {
            if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
        }

        return Ok(new UserDto
        {
            Username = user.UserName,
            Token = TokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
        });
    }

    private async Task<bool> UserExists(string username)
    {
        return await Context.Users.AnyAsync(u => u.UserName == username.ToLower());
    }
}