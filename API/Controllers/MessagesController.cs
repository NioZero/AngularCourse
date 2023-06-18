using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class MessagesController : BaseApiController
{
    private readonly IUserRepository UserRepository;
    private readonly IMessageRepository MessageRepository;
    private readonly IMapper Mapper;

    public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
    {
        UserRepository = userRepository;
        MessageRepository = messageRepository;
        Mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
    {
        var username = User.GetUsername();

        if(username == createMessageDto.RecipientUsername.ToLower())
            return BadRequest("You cannot send messages to yourself");

        var sender = await UserRepository.GetUserByUsername(username);
        var recipient = await UserRepository.GetUserByUsername(createMessageDto.RecipientUsername);

        var message = new Message()
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };

        MessageRepository.AddMessage(message);

        if(await MessageRepository.SaveAllAsync()) return Ok(Mapper.Map<MessageDto>(message));

        return BadRequest("Failed to send message");
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
    {
        messageParams.Username = User.GetUsername();

        var messages = await MessageRepository.GetMessagesForUser(messageParams);

        Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages));

        return messages;
    }

    [HttpGet("thread/{username}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
    {
        var currentUserName = User.GetUsername();

        return Ok(await MessageRepository.GetMessageThread(currentUserName, username));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMessage(int id)
    {
        var username = User.GetUsername();

        var message = await MessageRepository.GetMessage(id);

        if(message.SenderUsername != username && message.RecipientUsername != username)
            return Unauthorized();

        if(message.SenderUsername == username) message.SenderDeleted = true;
        if(message.RecipientUsername == username) message.RecipientDeleted = true;

        if(message.SenderDeleted && message.RecipientDeleted)
            MessageRepository.DeleteMessage(message);

        if(await MessageRepository.SaveAllAsync()) return Ok();

        return BadRequest("Problem deleting the message");
    }
}