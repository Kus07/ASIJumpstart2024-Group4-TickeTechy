using AutoMapper;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;

namespace ASI.Basecode.WebApp
{
    // AutoMapper configuration
    internal partial class StartupConfigurer
    {
        /// <summary>
        /// Configure auto mapper
        /// </summary>
        private void ConfigureAutoMapper()
        {
            var mapperConfiguration = new MapperConfiguration(config =>
            {
                config.AddProfile(new AutoMapperProfileConfiguration());
            });

            this._services.AddSingleton<IMapper>(sp => mapperConfiguration.CreateMapper());
        }

        private class AutoMapperProfileConfiguration : Profile
        {
            public AutoMapperProfileConfiguration()
            {
                CreateMap<LoginViewModel, User>();
                CreateMap<Ticket, TicketModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.StatusName))     
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.User.Username)) 
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt ?? DateTime.Now)) 
            .ForMember(dest => dest.AttachmentPath, opt => opt.MapFrom(src => src.Attachments))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.User.UserDetails.FirstOrDefault().FirstName))
            .ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.TicketAssigneds.FirstOrDefault().Agent.Username));
            }
        }
    }
}
