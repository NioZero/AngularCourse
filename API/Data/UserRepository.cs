using API.DTOs;
using API.Entities;
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

    public async Task<bool> SaveAllAsync()
    {
        return await Context.SaveChangesAsync() > 0;
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

    public async Task<IEnumerable<MemberDto>> GetMembersAsync()
    {
        return await Context.Users.ProjectTo<MemberDto>(Mapper.ConfigurationProvider).ToListAsync();
    }
}