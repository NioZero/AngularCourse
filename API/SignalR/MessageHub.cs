using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

[Authorize]
public class MessageHub : Hub
{
    private readonly IMessageRepository MessageRepository;
    private readonly IUserRepository UserRepository;
    private readonly IMapper Mapper;
    private readonly IHubContext<PresenceHub> PresenceHub;

    public MessageHub(IMessageRepository messageRepository, IUserRepository userRepository, IMapper mapper, IHubContext<PresenceHub> presenceHub)
    {
        MessageRepository = messageRepository;
        UserRepository = userRepository;
        Mapper = mapper;
        PresenceHub = presenceHub;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        var otherUser = httpContext.Request.Query["user"];

        var groupName = GetGroupName(Context.User.GetUsername(), otherUser);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var group = await AddToGroup(groupName);

        await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

        var messages = await MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

        await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var group = await RemoveFromMessageGroup();

        await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(CreateMessageDto createMessageDto)
    {
        var username = Context.User.GetUsername();

        if(username == createMessageDto.RecipientUsername.ToLower())
            throw new HubException("You cannot send messages to yourself");

        var sender = await UserRepository.GetUserByUsername(username);
        var recipient = await UserRepository.GetUserByUsername(createMessageDto.RecipientUsername);

        if(recipient==null) throw new HubException("Not found user");

        var message = new Message()
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };

        var groupName = GetGroupName(sender.UserName, recipient.UserName);

        var group = await MessageRepository.GetMessageGroup(groupName);

        if(group.Connections.Any(x => x.Username == recipient.UserName))
        {
            message.DateRead = DateTime.UtcNow;
        }
        else
        {
            var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
            if(connections != null)
            {
                await PresenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                new {
                    username = sender.UserName,
                    knownAs = sender.KnownAs
                });
            }
        }

        MessageRepository.AddMessage(message);

        if(await MessageRepository.SaveAllAsync()) 
        {
            await Clients.Groups(groupName).SendAsync("NewMessage", Mapper.Map<MessageDto>(message));
        }
    }

    private string GetGroupName(string caller, string other)
    {
        var stringCompare = string.CompareOrdinal(caller, other) < 0;

        return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
    }

    private async Task<Group> AddToGroup(string groupName)
    {
        var group = await MessageRepository.GetMessageGroup(groupName);

        var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

        if(group==null)
        {
            group = new Group(groupName);
            MessageRepository.AddGroup(group);
        }

        group.Connections.Add(connection);

        if(await MessageRepository.SaveAllAsync()) return group;

        throw new HubException("Failed to add to group");
    }

    private async Task<Group> RemoveFromMessageGroup()
    {
        var group = await MessageRepository.GetGroupForConnection(Context.ConnectionId);
        var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
        MessageRepository.RemoveConnection(connection);
        if(await MessageRepository.SaveAllAsync()) return group;

        throw new HubException("Failed to remove from group");
    }
}