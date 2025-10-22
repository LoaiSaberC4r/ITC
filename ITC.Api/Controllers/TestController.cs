using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using ITC.Application.Features.TestManagement.Command.Add;
using ITC.Application.Features.TestManagement.Query.GetTest;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ITC.Api.Controllers
{
    [Route("api/[controller]")]
    public class TestController : ControllerTemplate
    {
        public TestController(ISender sender) : base(sender)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetTest([FromQuery] GetTestQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }

        [HttpPost]
        public async Task<IActionResult> PostTest([FromBody] AddTestCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult(cancellationToken);
        }
    }
}