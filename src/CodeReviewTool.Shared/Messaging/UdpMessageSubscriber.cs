// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace CodeReviewTool.Shared.Messaging;

public class UdpMessageSubscriber : IMessageSubscriber, IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly ILogger<UdpMessageSubscriber> _logger;
    private readonly ConcurrentDictionary<Type, Delegate> _handlers;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _receiveTask;
    private bool _disposed;

    public UdpMessageSubscriber(int port, ILogger<UdpMessageSubscriber> logger)
    {
        _udpClient = new UdpClient(port);
        _logger = logger;
        _handlers = new ConcurrentDictionary<Type, Delegate>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UdpMessageSubscriber));
        }

        var messageType = typeof(TMessage);
        _handlers.TryAdd(messageType, handler);

        _logger.LogInformation("Subscribed to message type {MessageType}", messageType.Name);

        if (_receiveTask == null)
        {
            _receiveTask = Task.Run(async () => await ReceiveMessagesAsync(_cancellationTokenSource.Token));
        }

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<TMessage>()
        where TMessage : class
    {
        var messageType = typeof(TMessage);
        _handlers.TryRemove(messageType, out _);

        _logger.LogInformation("Unsubscribed from message type {MessageType}", messageType.Name);

        return Task.CompletedTask;
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started receiving messages");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync();
                var bytes = result.Buffer;

                await ProcessMessageAsync(bytes, cancellationToken);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                break;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error receiving message");
            }
        }

        _logger.LogInformation("Stopped receiving messages");
    }

    private async Task ProcessMessageAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var handlerEntry in _handlers)
            {
                var messageType = handlerEntry.Key;
                var handler = handlerEntry.Value;

                try
                {
                    var message = MessagePackSerializer.Deserialize(messageType, bytes);

                    if (message != null)
                    {
                        await ((dynamic)handler)((dynamic)message);
                        _logger.LogDebug("Processed message of type {MessageType}", messageType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message of type {MessageType}", messageType.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing message");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource?.Cancel();
            _receiveTask?.Wait(TimeSpan.FromSeconds(5));
            _udpClient?.Dispose();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}
