// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Net.Sockets;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace CodeReviewTool.Shared.Messaging;

public class UdpMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _endpoint;
    private readonly ILogger<UdpMessagePublisher> _logger;
    private bool _disposed;

    public UdpMessagePublisher(string host, int port, ILogger<UdpMessagePublisher> logger)
    {
        _udpClient = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UdpMessagePublisher));
        }

        try
        {
            var bytes = MessagePackSerializer.Serialize(message);
            await _udpClient.SendAsync(bytes, bytes.Length, _endpoint);

            _logger.LogDebug("Published message of type {MessageType} ({Size} bytes)", 
                typeof(TMessage).Name, bytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _udpClient?.Dispose();
            _disposed = true;
        }
    }
}
