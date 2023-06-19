using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository : IUserRepository
{
    private readonly DataContext Context;

    private readonly IMapper Mapper;

    public UserRepository(DataContext context, IMapper mapper)
    {
        Context = context;
        Mapper = mapper;
    }

    public async Task<AppUser> GetUserByIdAsync(int id)
    {
        return await Context.Users.FindAsync(id);
    }

    public async Task<AppUser> GetUserByUsername(string username)
    {
        return await Context.Users.Include(p => p.Photos).SingleOrDefaultAsync(u => u.UserName == username);
    }

    public async Task<IEnumerable<AppUser>> GetUsersAsync()
    {
        return await Context.Users
        .Include(p => p.Photos)
        .ToListAsync();
    }

    public void Update(AppUser user)
    {
        Context.Entry(user).State = EntityState.Modified;
    }

    public async Task<MemberDto> GetMemberAsync(string username)
    {
        return await Context.Users.Where(x => x.UserName == username)
            .ProjectTo<MemberDto>(Mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();
    }

    public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
    {
        var query = Context.Users.AsQueryable();

        query = query.Where(u => u.UserName != userParams.CurrentUsername);
        query = query.Where(u => u.Gender == userParams.Gender);

        var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
        var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

        query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

        query = userParams.OrderBy switch
        {
            "created" => query.OrderByDescending(u => u.Created),
            _ => query.OrderByDescending(u => u.LastActive)
        };

        return await PagedList<MemberDto>.CreateAsync(
            query.AsNoTracking().ProjectTo<MemberDto>(Mapper.ConfigurationProvider),
            userParams.pageNumber,
            userParams.PageSize);
    }

    public async Task<string> GetUserGender(string username)
    {
        return await Context.Users.Where(x => x.UserName == username).Select(x => x.Gender).FirstOrDefaultAsync();
    }
}