using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenCCG.Core;
using OpenCCG.Net;
using OpenCCG.Net.Rpc;

namespace OpenCCG;

public partial class EnemyHandArea : HBoxContainer, IMessageReceiver<MessageType>
{
    private static readonly PackedScene CardHiddenScene = GD.Load<PackedScene>("res://scenes/card-hidden.tscn");
    private readonly List<TextureRect> _cards = new();

    public Dictionary<string, IObserver>? Observers => null;

    [Rpc]
    public async void HandleMessageAsync(string messageJson)
    {
        await IMessageReceiver<MessageType>.HandleMessageAsync(this, messageJson);
    }

    public Executor GetExecutor(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.RemoveCard => Executor.Make(RemoveCard,Executor.ResponseMode.NoResponse),
            MessageType.DrawCard => Executor.Make(DrawCard,Executor.ResponseMode.NoResponse)
        };
    }

    private void RemoveCard()
    {
        var cardEntity = _cards.LastOrDefault();
        if (cardEntity == null) return;

        _cards.Remove(cardEntity);
        cardEntity.QueueFree();
    }

    private void DrawCard()
    {
        var entity = CardHiddenScene.Make<TextureRect>(this);
        _cards.Add(entity);
    }
}