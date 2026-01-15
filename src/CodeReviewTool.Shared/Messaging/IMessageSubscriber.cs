// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CodeReviewTool.Shared.Messaging;

public interface IMessageSubscriber
{
    Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default)
        where TMessage : class;

    Task UnsubscribeAsync<TMessage>() where TMessage : class;
}
