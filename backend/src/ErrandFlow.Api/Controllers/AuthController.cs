using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ErrandFlow.Api.Controllers;

    [ApiController]
    [Route("api/auth")]
    [Authorize]
    public sealed class AuthController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var externalUserId = User.FindFirstValue("sub");
            var localUserId = User.FindFirstValue("errandflow_user_id");

            return Ok(new
            {
                ExternalUserId = externalUserId,
                LocalUserId = localUserId,
                IsRegistered = localUserId is not null
            });
        }
    }
