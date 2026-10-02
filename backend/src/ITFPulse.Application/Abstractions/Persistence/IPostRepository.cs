using ITFPulse.Domain.Posts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Application.Abstractions.Persistence
{
    public interface IPostRepository
    {
        Task AddAsync(
            Post post,
            CancellationToken cancellationToken = default);
    }
}
