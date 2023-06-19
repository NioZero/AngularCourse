using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Interfaces;
using AutoMapper;

namespace API.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext Context;

    private readonly IMapper Mapper;

    public UnitOfWork(DataContext context, IMapper mapper)
    {
        Context = context;
        Mapper = mapper;
    }

    public IUserRepository UserRepository => new UserRepository(Context, Mapper);

    public IMessageRepository MessageRepository => new MessageRepository(Context, Mapper);

    public ILikesRepository LikesRepository => new LikesRepository(Context);

    public async Task<bool> Complete()
    {
        return await Context.SaveChangesAsync() > 0;
    }

    public bool HasChanges()
    {
        return Context.ChangeTracker.HasChanges();
    }
}