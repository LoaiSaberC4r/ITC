using FluentValidation;
using ITC.Domain.Resources;

namespace ITC.Application.Features.TestManagement.Command.Add
{
    public sealed class AddTestCommandValidation : AbstractValidator<AddTestCommand>
    {
        public AddTestCommandValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(Message.NotFount);
        }
    }
}