using Authors;
using Grpc.Core;

using Blog.Authors.gRPC.Data;
using Microsoft.EntityFrameworkCore;

namespace Blog.Authors.gRPC.Services
{
    public class AuthorsService : AuthorsRpc.AuthorsRpcBase, IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<AuthorsService> _logger;

        public AuthorsService(
            ApplicationContext context, 
            ILogger<AuthorsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task<GetByIdreply> GetById(GetByIdRequest request, ServerCallContext context)
        {
            var id = new Guid(request.Id);

            _logger.LogInformation("Obtendo o autor com id {AuthorId}", id);

            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id, context.CancellationToken);

            if (author is null)
            {
                _logger.LogWarning("O autor não foi encontrado");

                throw new RpcException(new Status(StatusCode.Internal, "Not Found"), "The author has not found");
            }

            _logger.LogInformation("Autor encontrado: {Author}", author);

            return new GetByIdreply { Id = request.Id, Name = author.Name };
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
