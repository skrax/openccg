using System.Linq;
using Godot;
using OpenCCG.Core;
using OpenCCG.Net.ServerNodes;

namespace OpenCCG;

public partial class Avatar : TextureRect
{
    [Export] public bool IsEnemy;

    public override void _Ready()
    {
        EventSink.OnDragForCombatStart += OnDragForCombatStart;
        EventSink.OnDragForCombatStop += OnDragForCombatStop;
        EventSink.OnDragSelectTargetStart += OnDragSelectTargetStart;
        EventSink.OnDragSelectTargetStop += OnDragSelectTargetStop;
    }

    public override void _ExitTree()
    {
        EventSink.OnDragForCombatStart -= OnDragForCombatStart;
        EventSink.OnDragForCombatStop -= OnDragForCombatStop;
        EventSink.OnDragSelectTargetStart -= OnDragSelectTargetStart;
        EventSink.OnDragSelectTargetStop -= OnDragSelectTargetStop;
    }

    private void OnDragSelectTargetStart(RequireTargetInputDto dto)
    {
        if (dto.Type is RequireTargetType.Creature) return;
        if (IsEnemy && dto.Side == RequireTargetSide.Friendly) return;
        if (!IsEnemy && dto.Side == RequireTargetSide.Enemy) return;

        DrawOutline(true);
    }

    private void OnDragSelectTargetStop()
    {
        DrawOutline(false);
    }

    private void OnDragForCombatStart(ulong instanceId)
    {
        if (!IsEnemy) return;
        var board = GetNode<BoardArea>("/root/Main/EnemyBoard");
        if (board._cards.Any(x => x.CardImplementationDto.CreatureAbilities!.Defender)) return;

        DrawOutline(true);
    }


    private void OnDragForCombatStop(ulong instanceId)
    {
        DrawOutline(false);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var obj = InstanceFromId(data.As<ulong>());

        return obj switch
        {
            CardBoard => IsEnemy,
            CardEffectPreview => true,
            _ => false
        };
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var obj = InstanceFromId(data.As<ulong>());

        switch (obj)
        {
            case CardBoard attacker:
            {
                Logger.Info<CardBoard>($"{attacker!.CardImplementationDto.Id} attacked avatar");

                GetNode<Main>("/root/Main").CombatPlayer(attacker.CardImplementationDto.Id);
                break;
            }
            case CardEffectPreview effect:
            {
                effect.TryUpstreamTarget(this);
                break;
            }
        }
    }

    private void DrawOutline(bool enabled)
    {
        var shader = Material as ShaderMaterial;
        shader?.SetShaderParameter("drawOutline", enabled);
    }
}