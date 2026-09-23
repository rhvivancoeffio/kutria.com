using MediatR;

namespace Commerce.Application.Common.Abstracts;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
