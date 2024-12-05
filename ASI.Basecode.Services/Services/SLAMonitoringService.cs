using System;
using System.Linq;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Manager;
using Basecode.Data.Repositories;

public class SLAMonitoringService
{
    private readonly BaseRepository<Ticket> _ticketRepo;
    private readonly BaseRepository<TicketAssigned> _ticketAssignedRepo;
    private readonly BaseRepository<Notification> _notificationRepo;
    private readonly BaseRepository<User> _userRepo;
    private readonly MailManager _mailManager;
    public static DateTime utcNow = DateTime.UtcNow;

    // Define the timezone offset for UTC+08:00
    public static TimeSpan utcOffset = TimeSpan.FromHours(8); // UTC+08:00

    // Apply the timezone offset to get the local time in UTC+08:00
    Nullable<DateTime> PHTIME = utcNow + utcOffset;

    public SLAMonitoringService(
        BaseRepository<Ticket> ticketRepo,
        BaseRepository<TicketAssigned> ticketAssignedRepo,
        BaseRepository<Notification> notificationRepo,
        BaseRepository<User> userRepo,
        MailManager mailManager)
    {
        _ticketRepo = ticketRepo;
        _ticketAssignedRepo = ticketAssignedRepo;
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
        _mailManager = mailManager;
    }

    public void MonitorTickets()
    {
        HandleOpenTickets();
        HandleOngoingTickets();
        HandleWaitingResponseTickets();
        HandleResolvedTickets();
    }

    private void HandleOpenTickets()
    {
        // Fetch OPEN tickets from the database
        var openTickets = _ticketRepo.Table
            .Where(t => t.StatusId == 1) // OPEN
            .ToList(); // Move to memory for further filtering

        // Filter tickets in memory
        var overdueTickets = openTickets
            .Where(t => PHTIME.Value.Subtract((t.UpdatedAt ?? t.CreatedAt).GetValueOrDefault()).TotalHours > 2)
            .ToList();

        foreach (var ticket in overdueTickets)
        {
            if (ticket.Priority == "Low")
                ticket.Priority = "Medium";
            else if (ticket.Priority == "Medium")
                ticket.Priority = "High";

            ticket.UpdatedAt = PHTIME;
            _ticketRepo.Update(ticket.Id, ticket);

            // Notify assigned agent
            var ticketAssigned = _ticketAssignedRepo.Table.FirstOrDefault(t => t.TicketId == ticket.Id);
            if (ticketAssigned != null)
            {
                CreateNotification((int)ticketAssigned.AgentId, $"SLA Escalation: Ticket #{ticket.Id} priority increased to {ticket.Priority}.", ticket.Id);

                // Send email to the agent
                var agent = _userRepo.Get(ticketAssigned.AgentId);
                if (agent != null && !string.IsNullOrEmpty(agent.Email))
                {
                    string errResponse = string.Empty;
                    bool emailSent = _mailManager.EmailRespond(
                        recipientEmail: agent.Email,
                        firstName: agent.Username,
                        ticketId: ticket.Id,
                        ticketMessage: "Priority has been escalated due to SLA breach.",
                        ref errResponse
                    );

                    if (!emailSent)
                    {
                        Console.WriteLine($"Failed to send email to {agent.Email}: {errResponse}");
                    }
                }
            }
        }
    }

    private void HandleOngoingTickets()
    {
        // Fetch ONGOING tickets from the database
        var ongoingTickets = _ticketRepo.Table
            .Where(t => t.StatusId == 2) // ONGOING
            .ToList(); // Move to memory for further filtering

        // Filter tickets in memory
        var overdueTickets = ongoingTickets
            .Where(t => PHTIME.Value.Subtract((t.UpdatedAt ?? t.CreatedAt).GetValueOrDefault()).TotalHours > 12)
            .ToList();

        foreach (var ticket in overdueTickets)
        {
            ticket.Priority = "High"; // Escalate to High
            ticket.UpdatedAt = PHTIME;
            _ticketRepo.Update(ticket.Id, ticket);

            // Notify agent or manager
            var ticketAssigned = _ticketAssignedRepo.Table.FirstOrDefault(t => t.TicketId == ticket.Id);
            if (ticketAssigned != null)
            {
                CreateNotification((int)ticketAssigned.AgentId, $"SLA Escalation: Ticket #{ticket.Id} is overdue for resolution.", ticket.Id);

                // Send email to the agent
                var agent = _userRepo.Get(ticketAssigned.AgentId);
                if (agent != null && !string.IsNullOrEmpty(agent.Email))
                {
                    string errResponse = string.Empty; // Initialize error response
                    bool emailSent = _mailManager.EmailRespond(
                        recipientEmail: agent.Email,
                        firstName: agent.Username,
                        ticketId: ticket.Id,
                        ticketMessage: "Ticket is overdue for resolution.",
                        ref errResponse
                    );

                    if (!emailSent)
                    {
                        Console.WriteLine($"Failed to send email to {agent.Email}: {errResponse}");
                    }
                }
            }
        }
    }

    private void HandleWaitingResponseTickets()
    {
        // Fetch WAITING RESPONSE tickets from the database
        var waitingTickets = _ticketRepo.Table
            .Where(t => t.StatusId == 3) // WAITINGRESPONSE
            .ToList(); // Move to memory for further filtering

        // Filter tickets in memory
        var overdueTickets = waitingTickets
            .Where(t => PHTIME.Value.Subtract((t.UpdatedAt ?? t.CreatedAt).GetValueOrDefault()).TotalHours > 24)
            .ToList();

        foreach (var ticket in overdueTickets)
        {
            CreateNotification((int)ticket.UserId, $"Action Required: Ticket #{ticket.Id} is awaiting your response.", ticket.Id);
        }
    }

    private void HandleResolvedTickets()
    {
        // Fetch RESOLVED tickets from the database
        var resolvedTickets = _ticketRepo.Table
            .Where(t => t.StatusId == 4) // RESOLVED
            .ToList(); // Move to memory for further filtering

        // Filter tickets in memory
        var overdueTickets = resolvedTickets
            .Where(t => PHTIME.Value.Subtract((t.UpdatedAt ?? t.CreatedAt).GetValueOrDefault()).TotalHours > 24)
            .ToList();

        foreach (var ticket in overdueTickets)
        {
            CreateNotification((int)ticket.UserId, $"Reminder: Please confirm if Ticket #{ticket.Id} is resolved.", ticket.Id);

            // Send email to the customer
            var user = _userRepo.Get(ticket.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                string errResponse = string.Empty; // Initialize error response
                bool emailSent = _mailManager.ResolveNotif(
                    recipientEmail: user.Email,
                    firstName: user.Username,
                    ticketId: ticket.Id,
                    ticketMessage: "Please confirm if the resolution is satisfactory.",
                    ref errResponse
                );

                if (!emailSent)
                {
                    Console.WriteLine($"Failed to send email to {user.Email}: {errResponse}");
                }
            }
        }
    }

    private void CreateNotification(int toUserId, string content, int ticketId)
    {
        var notification = new Notification
        {
            ToUserId = toUserId,
            Content = content,
            DateCreated = PHTIME,
            Status = "UNREAD",
            TicketId = ticketId,
            Title = $"Ticket #{ticketId}"
        };

        _notificationRepo.Create(notification);
    }
}
