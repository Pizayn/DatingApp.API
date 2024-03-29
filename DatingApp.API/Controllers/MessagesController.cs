﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Message = DatingApp.API.Models.Message;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Produces("application/json")]
    [Route("api/users/{userId}/Messages")]
    public class MessagesController : Controller
    {
        private IDatingRepository _repo;
        private IMapper _mapper;

        public MessagesController(IDatingRepository datingRepository, IMapper mapper)
        {
            _repo = datingRepository;
            _mapper = mapper;
        }
        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessage(id);
            if (messageFromRepo == null)
            {
                return BadRequest("Message could not find");
            }
            var messageToReturn = _mapper.Map<MessageForCreationDto>(messageFromRepo);
            return Ok(messageToReturn);
        }
        [HttpGet]
        public async Task<IActionResult> GetMessageForUser(int userId, [FromQuery]MessageParams messageParams)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            messageParams.UserId = userId;
            var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);
            var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);
            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize, messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);
            return Ok(messages);

        }
        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var messages = await _repo.GetMessageThread(userId, recipientId);
            var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messages);
            return Ok(messageThread);
        }
        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, [FromBody]MessageForCreationDto messageForCreation)
        {
            var sender = await _repo.GetUser(userId);

            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            messageForCreation.SenderId = userId;
            var recipient = await _repo.GetUser(messageForCreation.RecipientId);
            if (recipient == null)
                return BadRequest("Could not find user");
            var message = _mapper.Map<Message>(messageForCreation);
            _repo.Add(message);
            if (await _repo.SaveAll())
            {
                var messageToReturn = _mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);
            }
            throw new Exception("Creating the message failed on save");
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var message = await _repo.GetMessage(id);
            if (message.SenderId == userId)
            {
                message.IsDeletedBySender = true;
            }
            if (message.RecipientId == userId)
            {
                message.IsDeletedByRecipient = true;
            }
            if (message.IsDeletedByRecipient && message.IsDeletedBySender)
            {
                _repo.Delete(message);
            }
            if (await _repo.SaveAll())
                return NoContent();
            throw new Exception("Error deleting the message");
        }
        [HttpPost("{id}/read")]
        public async Task<IActionResult> Read(int id, int userId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var message = await _repo.GetMessage(id);
            if (message.RecipientId != userId)
                return Unauthorized();
            message.IsRead = true;
            message.DateRead = DateTime.Now;
            await _repo.SaveAll();
            return NoContent();
        }
    }
}