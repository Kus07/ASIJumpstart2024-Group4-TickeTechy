using ASI.Basecode.Resources.Messages;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ASI.Basecode.WebApp.Controllers
{
    public class TicketController : BaseController
    {

        private readonly IUserService _userService;

        public TicketController(MailManager mailManager, IUserService userService) : base(mailManager)
        {
            _userService = userService;
        }

        // to be followed

    }
}
