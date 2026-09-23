using MediatR;

namespace Commerce.Application.Common.Abstracts;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
