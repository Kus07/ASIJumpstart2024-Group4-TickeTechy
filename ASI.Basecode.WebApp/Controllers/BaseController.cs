using ASI.Basecode.Data.Models;
using ASI.Basecode.Data;
using ASI.Basecode.Services.Manager;
using Basecode.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

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
        public readonly MailManager _mailManager;

        public BaseController(MailManager mailManager)
        {
            _mailManager = mailManager;
            _db = new AllianceJumpstartContext();
            _userRepo = new BaseRepository<User>();
            _roleRepo = new BaseRepository<Role>();
            _articleRepo = new BaseRepository<Article>();
            _ticketRepo = new BaseRepository<Ticket>();
            _ticketAssignedRepo = new BaseRepository<TicketAssigned>();
            _categoryRepo = new BaseRepository<Category>();
            _feedbackRepo = new BaseRepository<Feedback>();
            _userDetailRepo = new BaseRepository<UserDetail>();
        }
    }
}
