using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoAPI.Models;
using SistemaVotoAPI.Services.EmailServices;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailServices _emailServices;

        public EmailController(IEmailServices emailServices)
        {
            _emailServices = emailServices;
        }

        [HttpPost]
        public IActionResult SendEmail(EmailDto request)
        {
            _emailServices.SendEmail(request);


            return Ok();

        }
    }
}
