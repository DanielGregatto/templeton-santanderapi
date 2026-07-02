using FluentValidation;

namespace Services.Features.Stories.Queries.GetBestStories
{
    public class GetBestStoriesQueryValidator : AbstractValidator<GetBestStoriesQuery>
    {
        public GetBestStoriesQueryValidator()
        {
            RuleFor(x => x.N)
                .GreaterThan(0).WithMessage("n must be greater than 0.");
        }
    }
}
