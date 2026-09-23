using MediatR;

namespace Commerce.Application.Common.Abstracts;

public interface IQuery<out TResponse> : IRequest<TResponse>;
