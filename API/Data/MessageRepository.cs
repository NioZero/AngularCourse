using System.Net.Mime;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository : IMessageRepository
{
    private readonly DataContext Context;
    private readonly IMapper Mapper;

    public MessageRepository(DataContext context, IMapper mapper)
    {
        Context = context;
        Mapper = mapper;
    }

    public void AddMessage(Message message)
    {
        Context.Messages.Add(message);
    }

    public void DeleteMessage(Message message)
    {
        Context.Messages.Remove(message);
    }

    public async Task<Message> GetMessage(int id)
    {
        return await Context.Messages.FindAsync(id);
    }

    public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
    {
        var query = Context.Messages.OrderByDescending(m => m.MessageSent).AsQueryable();

        query = messageParams.Container switch
        {
            "Inbox" => query.Where(m => m.RecipientUsername == messageParams.Username && !m.RecipientDeleted),
            "Outbox" => query.Where(m => m.SenderUsername == messageParams.Username && !m.SenderDeleted),
            _ => query.Where(m => m.RecipientUsername == messageParams.Username && !m.DateRead.HasValue && !m.RecipientDeleted)
        };

        var messages = query.ProjectTo<MessageDto>(Mapper.ConfigurationProvider);

        return await PagedList<MessageDto>.CreateAsync(messages, messageParams.pageNumber, messageParams.PageSize);
    }

    public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string recipientUsername)
    {
        var messages = await Context.Messages
            .Include(u => u.Sender).ThenInclude(u => u.Photos)
            .Include(u => u.Recipient).ThenInclude(u => u.Photos)
            .Where(m =>
                (m.RecipientUsername == currentUserName && m.SenderUsername == recipientUsername && !m.RecipientDeleted)
                || (m.RecipientUsername == recipientUsername && m.SenderUsername == currentUserName && !m.SenderDeleted)
            )
            .OrderBy(m => m.MessageSent).ToListAsync();

        var unreadMessages = messages.Where(m => !m.DateRead.HasValue && m.RecipientUsername == currentUserName).ToList();

        if(unreadMessages.Any())
        {
            foreach(var message in unreadMessages)
            {
                message.DateRead = DateTime.UtcNow;
            }

            await Context.SaveChangesAsync();
        }

        return Mapper.Map<IEnumerable<MessageDto>>(messages);
    }

    public async Task<bool> SaveAllAsync()
    {
        return await Context.SaveChangesAsync() > 0;
    }

    public void AddGroup(Group group)
    {
        Context.Groups.Add(group);
    }

    public void RemoveConnection(Connection connection)
    {
        Context.Connections.Remove(connection);
    }

    public async Task<Connection> GetConnection(string connectionId)
    {
        return await Context.Connections.FindAsync(connectionId);
    }
    
    public async Task<Group> GetMessageGroup(string groupName)
    {
        return await Context.Groups.Include(c => c.Connections).FirstOrDefaultAsync(g => g.Name == groupName);
    }

    public async Task<Group> GetGroupForConnection(string connectionId)
    {
        return await Context.Groups.Include(c => c.Connections).Where(g => g.Connections.Any(c => c.ConnectionId == connectionId)).FirstOrDefaultAsync();
    }
}