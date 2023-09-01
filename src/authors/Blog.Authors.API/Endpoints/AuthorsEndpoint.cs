using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Blog.Authors.API.Data;
using Blog.Authors.API.Models;

namespace Blog.Authors.API.Endpoints
{
    public static class AuthorsEndpoint
    {
        public static IEndpointRouteBuilder MapAuthors(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            const string BasePath = "/authors";

            endpointRouteBuilder
                .MapGet(BasePath, GetAllAsync)
                .WithName("GetAll");
            
            endpointRouteBuilder
                .MapPost(BasePath, CreateAsync)
                .WithName("Create");

            endpointRouteBuilder
                .MapGet(BasePath + "/{id:guid}", GetByIdAsync)
                .WithName("GetById");

            endpointRouteBuilder
                .MapPut(BasePath + "/{id:guid}", UpdateAsync)
                .WithName("Update");

            endpointRouteBuilder
                .MapDelete(BasePath + "/{id:guid}", DeleteAsync)
                .WithName("Delete");

            return endpointRouteBuilder;
        }

        private static async Task<IResult> GetAllAsync(
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEndpoint.GetAllAsync");

            logger.LogInformation("Obtendo os autores do banco");

            var authors = await dbContext
                .Authors
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            logger.LogInformation("Authores encontrados: {Count}", authors.Count);

            return authors.Count == 0
                ? Results.NoContent()
                : Results.Ok(authors);
        }

        private static async Task<IResult> CreateAsync(
            [FromBody] Author author,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEndpoint.CreateAsync");

            logger.LogInformation("Inserindo autor no banco");

            await dbContext.Authors.AddAsync(author, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.CreatedAtRoute("GetById", new { id = author.Id }, author);
        }

        private static async Task<IResult> GetByIdAsync(
            [FromRoute] Guid id,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEndpoint.GetByIdAsync");

            logger.LogInformation("Obtendo o autor com id {AuthorId}", id);

            var author = await dbContext
                .Authors
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            logger.LogInformation("Author encontrador: {Author}", author);

            return author is not null
                ? Results.Ok(author)
                : Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Author not found",
                });
        }

        private static async Task<IResult> UpdateAsync(
            [FromRoute] Guid id,
            [FromBody] Author author,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEndpoint.UpdateAsync");

            logger.LogInformation("Obtendo o autor com id {AuthorId}", id);

            var foundAuthor = await dbContext
                .Authors
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (foundAuthor is null)
            {
                logger.LogWarning("O autor não foi encontrado");

                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Author not found",
                });
            }

            foundAuthor.Name = author.Name;

            logger.LogInformation("Atualizando o autor no banco");

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(foundAuthor);
        }

        private static async Task<IResult> DeleteAsync(
            [FromRoute] Guid id,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEndpoint.DeleteAsync");

            logger.LogInformation("Obtendo o autor com id {AuthorId}", id);
            
            var author = await dbContext
                .Authors
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (author is null)
            {
                logger.LogWarning("O autor não foi encontrado");
                
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Author not found",
                });
            }

            dbContext.Authors.Remove(author);

            logger.LogInformation("Removendo o autor no banco");

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        }
    }
}
