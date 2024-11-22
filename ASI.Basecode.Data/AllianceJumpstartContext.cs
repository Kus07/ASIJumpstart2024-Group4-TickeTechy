using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ASI.Basecode.Data.Models
{
    public partial class AllianceJumpstartContext : DbContext
    {
        public AllianceJumpstartContext()
        {
        }

        public AllianceJumpstartContext(DbContextOptions<AllianceJumpstartContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Article> Articles { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Feedback> Feedbacks { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Status> Statuses { get; set; }
        public virtual DbSet<Ticket> Tickets { get; set; }
        public virtual DbSet<TicketAssigned> TicketAssigneds { get; set; }
        public virtual DbSet<TicketMessage> TicketMessages { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserDetail> UserDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseLazyLoadingProxies().UseSqlServer("Server=DESKTOP-SKPR52B\\SQLEXPRESS;Database=AllianceJumpstart;Trusted_Connection=True;Integrated Security=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
            modelBuilder.Entity<Article>(entity =>
            {
                entity.ToTable("Article");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Attachments).HasColumnName("attachments");

                entity.Property(e => e.Author)
                    .HasMaxLength(50)
                    .HasColumnName("author");

                entity.Property(e => e.CategoryId).HasColumnName("categoryId");

                entity.Property(e => e.Content).HasColumnName("content");

                entity.Property(e => e.LastmodifiedDate)
                    .HasColumnType("datetime")
                    .HasColumnName("lastmodifiedDate");

                entity.Property(e => e.PublishDate)
                    .HasColumnType("datetime")
                    .HasColumnName("publishDate");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");

                entity.Property(e => e.Title)
                    .HasMaxLength(50)
                    .HasColumnName("title");

                entity.Property(e => e.UserDetailId).HasColumnName("userDetailId");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Articles)
                    .HasForeignKey(d => d.CategoryId)
                    .HasConstraintName("FK_Article_Category");

                entity.HasOne(d => d.UserDetail)
                    .WithMany(p => p.Articles)
                    .HasForeignKey(d => d.UserDetailId)
                    .HasConstraintName("FK_Article_Article");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CategoryName)
                    .HasMaxLength(50)
                    .HasColumnName("categoryName");
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Department");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DepartmentName)
                    .HasMaxLength(50)
                    .HasColumnName("departmentName");
            });

            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.ToTable("Feedback");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AgentId).HasColumnName("agent_id");

                entity.Property(e => e.Comments).HasColumnName("comments");

                entity.Property(e => e.Star).HasColumnName("star");

                entity.Property(e => e.TicketId).HasColumnName("ticket_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Agent)
                    .WithMany(p => p.FeedbackAgents)
                    .HasForeignKey(d => d.AgentId)
                    .HasConstraintName("FK_Feedback_User1");

                entity.HasOne(d => d.Ticket)
                    .WithMany(p => p.Feedbacks)
                    .HasForeignKey(d => d.TicketId)
                    .HasConstraintName("FK_Feedback_TIcket");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.FeedbackUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Feedback_User");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notification");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Content).HasColumnName("content");

                entity.Property(e => e.DateCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("dateCreated");

                entity.Property(e => e.FromUserId).HasColumnName("from_user_id");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");

                entity.Property(e => e.TicketId).HasColumnName("ticket_id");

                entity.Property(e => e.Title)
                    .HasMaxLength(50)
                    .HasColumnName("title");

                entity.Property(e => e.ToUserId).HasColumnName("to_user_id");

                entity.HasOne(d => d.FromUser)
                    .WithMany(p => p.NotificationFromUsers)
                    .HasForeignKey(d => d.FromUserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Notification_User");

                entity.HasOne(d => d.Ticket)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.TicketId)
                    .HasConstraintName("FK_Notification_Ticket");

                entity.HasOne(d => d.ToUser)
                    .WithMany(p => p.NotificationToUsers)
                    .HasForeignKey(d => d.ToUserId)
                    .HasConstraintName("FK_Notification_User1");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.RoleName)
                    .HasMaxLength(50)
                    .HasColumnName("roleName");
            });

            modelBuilder.Entity<Status>(entity =>
            {
                entity.ToTable("Status");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.StatusName)
                    .HasMaxLength(50)
                    .HasColumnName("statusName");
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("Ticket");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Attachments).HasColumnName("attachments");

                entity.Property(e => e.CategoryId).HasColumnName("category_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Priority)
                    .HasMaxLength(50)
                    .HasColumnName("priority");

                entity.Property(e => e.Reassigned).HasColumnName("reassigned");

                entity.Property(e => e.StatusId).HasColumnName("status_id");

                entity.Property(e => e.Summary)
                    .IsUnicode(false)
                    .HasColumnName("summary");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("updated_at");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Tickets)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Ticket_Category");

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.Tickets)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Ticket_Status");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Tickets)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Ticket_User");
            });

            modelBuilder.Entity<TicketAssigned>(entity =>
            {
                entity.ToTable("TicketAssigned");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AgentId).HasColumnName("agent_id");

                entity.Property(e => e.ReassignedToId).HasColumnName("reassigned_to_id");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");

                entity.Property(e => e.TicketId).HasColumnName("ticket_id");

                entity.HasOne(d => d.Agent)
                    .WithMany(p => p.TicketAssignedAgents)
                    .HasForeignKey(d => d.AgentId)
                    .HasConstraintName("FK_TicketAssigned_User");

                entity.HasOne(d => d.ReassignedTo)
                    .WithMany(p => p.TicketAssignedReassignedTos)
                    .HasForeignKey(d => d.ReassignedToId)
                    .HasConstraintName("FK_TicketAssigned_User1");

                entity.HasOne(d => d.Ticket)
                    .WithMany(p => p.TicketAssigneds)
                    .HasForeignKey(d => d.TicketId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TicketAssigned_Ticket");
            });

            modelBuilder.Entity<TicketMessage>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.Message).HasColumnName("message");

                entity.Property(e => e.TicketId).HasColumnName("ticket_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Ticket)
                    .WithMany(p => p.TicketMessages)
                    .HasForeignKey(d => d.TicketId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TicketMessages_Ticket");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.TicketMessages)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_TicketMessages_User");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ArticleViewSetting).HasColumnName("articleViewSetting");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                entity.Property(e => e.Email)
                    .HasMaxLength(50)
                    .HasColumnName("email");

                entity.Property(e => e.EmailNotificationSetting).HasColumnName("emailNotificationSetting");

                entity.Property(e => e.ForgotPassOtp)
                    .HasMaxLength(50)
                    .HasColumnName("forgotPassOTP");

                entity.Property(e => e.Password).HasColumnName("password");

                entity.Property(e => e.RoleId).HasColumnName("role_id");

                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .HasColumnName("username");

                entity.HasOne(d => d.Department)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.DepartmentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_User_Department");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_User_Role");
            });

            modelBuilder.Entity<UserDetail>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ContactNo)
                    .HasMaxLength(50)
                    .HasColumnName("contactNo");

                entity.Property(e => e.FirstName)
                    .HasMaxLength(50)
                    .HasColumnName("firstName");

                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .HasColumnName("lastName");

                entity.Property(e => e.ProfilePicturePath).HasColumnName("profilePicturePath");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Users)
                    .WithMany(p => p.UserDetails)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_UserDetails_User");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
