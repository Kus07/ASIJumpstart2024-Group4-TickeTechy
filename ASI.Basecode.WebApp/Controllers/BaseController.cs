using ASI.Basecode.Data.Models;
using ASI.Basecode.Data;
using ASI.Basecode.Services.Manager;
using Basecode.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ASI.Basecode.WebApp.Controllers
{
    public class BaseController : Controller
    {
        public AllianceJumpstartContext _db;
        public BaseRepository<User> _userRepo;
        public BaseRepository<UserDetail> _userDetailRepo;
        public BaseRepository<Role> _roleRepo;
        public BaseRepository<Article> _articleRepo;
        public BaseRepository<Ticket> _ticketRepo;
        public BaseRepository<TicketAssigned> _ticketAssignedRepo;
        public BaseRepository<Category> _categoryRepo;
        public BaseRepository<Feedback> _feedbackRepo;
        public BaseRepository<TicketMessage> _ticketMessageRepo;
        public readonly MailManager _mailManager;

        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected ISession _session => _httpContextAccessor.HttpContext.Session;


        public BaseController(MailManager mailManager, IHttpContextAccessor httpContextAccessor)
        {
            _mailManager = mailManager;
            this._httpContextAccessor = httpContextAccessor;
            _db = new AllianceJumpstartContext();
            _userRepo = new BaseRepository<User>();
            _roleRepo = new BaseRepository<Role>();
            _articleRepo = new BaseRepository<Article>();
            _ticketRepo = new BaseRepository<Ticket>();
            _ticketAssignedRepo = new BaseRepository<TicketAssigned>();
            _categoryRepo = new BaseRepository<Category>();
            _feedbackRepo = new BaseRepository<Feedback>();
            _userDetailRepo = new BaseRepository<UserDetail>();
            _ticketMessageRepo = new BaseRepository<TicketMessage>();
        }
    }
}
