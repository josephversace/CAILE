using IIM.Core.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Application.Models
{
    public class ApplyModelTemplateCommand : IRequest<ModelTemplate>
    {
        public string TemplateId { get; }
        public string? UserId { get; }

        public ApplyModelTemplateCommand(string templateId, string? userId = null)
        {
            TemplateId = templateId;
            UserId = userId;
        }
    }

}
