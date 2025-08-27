using IIM.Core.Mediator;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Application.Models
{
    public class ApplyModelTemplateHandler : IRequestHandler<ApplyModelTemplateCommand, ModelTemplate>
    {
        // Inject services for models, orchestrator, DB, etc.

        public async Task<ModelTemplate> Handle(ApplyModelTemplateCommand request, CancellationToken ct)
        {
            // 1. Lookup template in DB
            // 2. Determine which models need to be loaded/unloaded
            // 3. Call orchestrator to load/unload as needed
            // 4. Optionally update DB or cache active template
            // 5. Return template info

            // (Implement your logic here)
            await Task.Delay(1000);

            return new ModelTemplate();
        }
    }

}
