using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Infrastructure.Messaging.Consumers;

namespace Tahdeera.Infrastructure.Messaging;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddMessagingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMessageOutboxPublisher, MassTransitOutboxPublisher>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<SendWhatsAppMessageConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"];
                if (!string.IsNullOrWhiteSpace(host))
                {
                    var port = ushort.TryParse(configuration["RabbitMq:Port"], out var p) ? p : (ushort)5672;
                    var username = configuration["RabbitMq:Username"] ?? "guest";
                    var password = configuration["RabbitMq:Password"] ?? "guest";

                    cfg.Host(host, port, "/", h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });
                }
                else
                {
                    var rabbitUri = configuration.GetConnectionString("RabbitMQ")
                        ?? "amqp://tahdeera_mq:SetSecurePassword@localhost:5672";

                    cfg.Host(new Uri(rabbitUri));
                }

                cfg.ReceiveEndpoint("whatsapp-delivery-queue", e =>
                {
                    e.PrefetchCount = 32;
                    e.ConcurrentMessageLimit = 16;

                    // Automatically routes unhandled poison exceptions to error DLQ
                    e.ConfigureConsumer<SendWhatsAppMessageConsumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
