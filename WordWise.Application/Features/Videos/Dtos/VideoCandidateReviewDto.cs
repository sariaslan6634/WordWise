using WordWise.Application.Features.Words.Dtos;

namespace WordWise.Application.Features.Videos.Dtos;

public class VideoCandidateReviewDto
{
    public VideoCandidateDto Candidate { get; set; } = new();
    public WordDetailDto Word { get; set; } = new();
}
