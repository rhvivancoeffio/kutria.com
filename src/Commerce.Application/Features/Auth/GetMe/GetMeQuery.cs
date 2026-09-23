using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Auth.GetMe;

public sealed record GetMeQuery(string AccessToken) : IQuery<GetMeResult>;
