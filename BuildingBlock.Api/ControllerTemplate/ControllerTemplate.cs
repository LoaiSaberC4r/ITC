using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlock.Api.ControllerTemplate
{
    [ApiController]
    public abstract class ControllerTemplate : ControllerBase
    {
        public readonly ISender sender;

        protected ControllerTemplate(ISender sender)
        {
            this.sender = sender;
        }
    }
}