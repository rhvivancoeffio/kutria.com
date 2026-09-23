using MediatR;

namespace Commerce.Application.Common.Abstracts;

public interface ICommand<out TResponse> : IRequest<TResponse>;
