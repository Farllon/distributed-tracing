using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static Blog.Posts.API.Rabbit;

namespace Blog.Posts.API
{
    public class Rabbit
    {
        public class RabbitModelPooledObjectPolicy : IPooledObjectPolicy<IModel>
        {
            private readonly RabbitOptions _options;

            private readonly IConnection _connection;

            public RabbitModelPooledObjectPolicy(IOptions<RabbitOptions> optionsAccs)
            {
                _options = optionsAccs.Value;
                _connection = GetConnection();
            }

            private IConnection GetConnection()
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _options.HostName,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    Port = _options.Port,
                    VirtualHost = _options.VHost,
                };

                return factory.CreateConnection();
            }

            public IModel Create()
            {
                return _connection.CreateModel();
            }

            public bool Return(IModel obj)
            {
                if (obj.IsOpen)
                {
                    return true;
                }
                else
                {
                    obj?.Dispose();

                    return false;
                }
            }
        }

        public class RabbitOptions
        {
            public string UserName { get; set; } = default!;

            public string Password { get; set; } = default!;

            public string HostName { get; set; } = default!;

            public int Port { get; set; } = 5672;

            public string VHost { get; set; } = "/";
        }

        public interface IRabbitManager
        {
            void Publish<T>(T message, string exchangeName, string exchangeType, string routeKey)
                where T : class;
        }

        public class RabbitManager : IRabbitManager
        {
            private readonly ILogger<RabbitManager> _logger;
            private readonly DefaultObjectPool<IModel> _objectPool;

            private static readonly ActivitySource _activitySource = new(nameof(IRabbitManager));
            private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

            public RabbitManager(
                ILogger<RabbitManager> logger, 
                IPooledObjectPolicy<IModel> objectPolicy)
            {
                _logger = logger;
                _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
            }

            public void Publish<T>(T message, string exchangeName, string exchangeType, string routeKey)
                where T : class
            {
                if (message == null)
                    return;

                var channel = _objectPool.Get();

                try
                {
                    channel.ExchangeDeclare(exchangeName, exchangeType, true, false, null);
                    
                    var sendBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                    using var activity = _activitySource.StartActivity("message send", ActivityKind.Producer);

                    var properties = channel.CreateBasicProperties();

                    properties.Persistent = true;

                    ActivityContext contextToInject = activity?.Context ?? Activity.Current?.Context ?? default;

                    _logger.LogInformation("Injetando as informaçãoes de tracing do contexto no header da mensagem");

                    _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, InjectTraceContext);

                    _logger.LogInformation("Header da mensagem: {Header}", properties.Headers);

                    channel.BasicPublish(exchangeName, routeKey, properties, sendBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocorreu um erro na publicação da mensagem do tipo {Type}", typeof(T).Name);

                    throw;
                }
                finally
                {
                    _objectPool.Return(channel);
                }
            }

            private void InjectTraceContext(IBasicProperties properties, string key, string value)
            {
                properties.Headers ??= new Dictionary<string, object>();
                properties.Headers[key] = value;
            }
        }
    }

    public static class RabbitServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbit(this IServiceCollection services, IConfiguration configuration)
        {
            var rabbitConfig = configuration.GetSection("RabbitMQ");

            services.Configure<RabbitOptions>(rabbitConfig);

            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<IPooledObjectPolicy<IModel>, RabbitModelPooledObjectPolicy>();

            services.AddSingleton<IRabbitManager, RabbitManager>();

            return services;
        }
    }
}
