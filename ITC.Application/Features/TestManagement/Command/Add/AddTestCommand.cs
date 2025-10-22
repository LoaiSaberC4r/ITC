using BuildingBlock.Application.Abstraction;

namespace ITC.Application.Features.TestManagement.Command.Add
{
    public sealed record AddTestCommand : ICommand, ICacheInvalidator
    {
        public string Name { get; init; } = string.Empty;

        public IEnumerable<string> Tags => new[] { "user" };
    }
}