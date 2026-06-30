using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Enums;
using static WordWise.Application.Features.Word.Queries.GetWordsList;

namespace WordWise.Application.Features.Word.Queries
{
    public record GetWordsList : IRequest<List<GetWordsListDto>>
    {
        public record GetWordsListDto(
             string Text ,
         string Definition ,
         string? Ipa ,
         string? PartOfSpeech ,
         CefrLevel? CefrLevel ,
         string? ExampleSentencesJson ,

         string? SynonymsJson ,
         string? AntonymsJson ,
         string? Category ,
         string? SourceProvider ,
         string? SourceUrl,
         DateTime? ImportedAt,
         bool IsPublished);
    }
    public class GetWordsListHandler(IWordWiseDbContext _context, IMapper _mapper) : IRequestHandler<GetWordsList, List<GetWordsListDto>>
    {
        public async Task<List<GetWordsListDto>> Handle(GetWordsList request, CancellationToken cancellationToken)
        {
            var wordsList = await _context.Words
                .AsNoTracking().ToListAsync(cancellationToken);

            var dtoResult = _mapper.Map<List<GetWordsListDto>>(wordsList);

            return dtoResult;
        }
    }
}
