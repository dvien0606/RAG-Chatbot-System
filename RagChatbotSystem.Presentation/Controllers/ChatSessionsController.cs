using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RagChatbotSystem.Business.DTOs;
using RagChatbotSystem.Business.Interfaces;

namespace RagChatbotSystem.Presentation.Controllers
{
    [ApiController]
    [Route("api/chat-sessions")]
    public class ChatSessionsController : ControllerBase
    {
        private readonly IChatSessionService _chatSessionService;

        public ChatSessionsController(IChatSessionService chatSessionService)
        {
            _chatSessionService = chatSessionService;
        }

        [HttpGet("{sessionId:guid}")]
        public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
        {
            var session = await _chatSessionService.GetSessionAsync(sessionId, cancellationToken);
            return session == null ? NotFound() : Ok(session);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _chatSessionService.CreateSessionAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetSession), new { sessionId = session.SessionId }, session);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{sessionId:guid}/messages")]
        public async Task<IActionResult> GetMessageHistory(Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var messages = await _chatSessionService.GetMessageHistoryAsync(sessionId, cancellationToken);
                return Ok(messages);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
