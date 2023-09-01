using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;

using Blog.Posts.API.Data;
using Blog.Posts.API.Models;
using Authors;
using Grpc.Core;
using static Blog.Posts.API.Rabbit;
using Blog.Posts.API.Events;

namespace Blog.Posts.API.Endpoints
{
    public static class PostsEndpoint
    {
        public static IEndpointRouteBuilder MapPosts(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            const string BasePath = "/posts";

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
            var logger = loggerFactory.CreateLogger("AuthorsEnpoint.GetAllAsync");

            logger.LogInformation("Obtendo os posts do banco");

            var posts = await dbContext
                .Posts
                .Find(FilterDefinition<Post>.Empty)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Posts encontrados: {Quantity}", posts.Count);

            return posts.Count == 0
                ? Results.NoContent()
                : Results.Ok(posts);
        }

        private static async Task<IResult> CreateAsync(
            [FromBody] Post post,
            [FromServices] AuthorsRpc.AuthorsRpcClient authorsService,
            [FromServices] ApplicationContext dbContext,
            [FromServices] IRabbitManager rabbitManager,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEnpoint.CreateAsync");

            string author = string.Empty;

            try
            {
                logger.LogInformation("Obtendo o autor pelo id: {AuthorId}", post.AuthorId);

                var authorReply = await authorsService.GetByIdAsync(
                    new GetByIdRequest { Id = post.AuthorId.ToString() },
                    cancellationToken: cancellationToken);

                logger.LogInformation("O autor encontrado foi: {AuthorName}", authorReply.Name);

                author = authorReply.Name;
            }
            catch (RpcException e)
            {
                logger.LogError(e, "Houve um erro ao buscar o author: {AuthorId}", post.AuthorId);

                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = e.Status.Detail,
                    Detail = e.Message
                });
            }

            logger.LogInformation("Inserindo o post no banco");

            await dbContext.Posts.InsertOneAsync(post, cancellationToken: cancellationToken);

            logger.LogInformation("Publicando o evento de post criado");

            rabbitManager.Publish(
                new PostCreated
                {
                    Author = author,
                    Title = post.Title
                },
                "exchange.topic.post-created",
                "topic",
                string.Empty);

            return Results.CreatedAtRoute("GetById", new { id = post.Id }, post);
        }

        private static async Task<IResult> GetByIdAsync(
            [FromRoute] Guid id,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEnpoint.GetByIdAsync");

            logger.LogInformation("Obtendo o post pelo id: {PostId}", id);

            var post = await dbContext
                .Posts
                .Find(Builders<Post>.Filter.Eq(a => a.Id, id))
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogInformation("Resultado obtido: {Result}", post);

            return post is not null
                ? Results.Ok(post)
                : Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Post not found",
                });
        }

        private static async Task<IResult> UpdateAsync(
            [FromRoute] Guid id,
            [FromBody] Post post,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEnpoint.UpdateAsync");

            var filter = Builders<Post>.Filter.Eq(a => a.Id, id);

            logger.LogInformation("Obtendo o post pelo id: {PostId}", id);

            var foundPost = await dbContext
                .Posts
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

            if (foundPost is null)
            {
                logger.LogWarning("O post não foi encontrado");

                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Post not found",
                });
            }

            foundPost.Title = post.Title;
            foundPost.Content = post.Content;

            logger.LogInformation("Atualizando o post no banco");

            await dbContext.Posts.ReplaceOneAsync(filter, post, cancellationToken: cancellationToken);

            return Results.Ok(foundPost);
        }

        private static async Task<IResult> DeleteAsync(
            [FromRoute] Guid id,
            [FromServices] ApplicationContext dbContext,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger("AuthorsEnpoint.DeleteAsync");

            logger.LogInformation("Obtendo o post pelo id: {PostId}", id);

            var filter = Builders<Post>.Filter.Eq(a => a.Id, id);

            var post = await dbContext
                .Posts
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

            if (post is null)
            {
                logger.LogWarning("O post não foi encontrado");

                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Post not found",
                });
            }

            logger.LogInformation("Removendo o post no banco");

            await dbContext.Posts.DeleteOneAsync(filter, cancellationToken);

            return Results.NoContent();
        }
    }
}
