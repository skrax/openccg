using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenCCG.Net;
using OpenCCG.Net.ServerNodes;

namespace OpenCCG.Data;

public class RandomDamageCardEffect : ICardEffect
{
    public const string Id = "TEST-E-005";

    public RandomDamageCardEffect(string initJson)
    {
        var init = JsonSerializer.Deserialize<Init>(initJson);

        Damage = init?.Damage ?? throw new ArgumentNullException();
        TargetSide = init.Side;
        TargetType = init.Type;
        Count = init.Count;
    }

    public int Damage { get; }

    public int Count { get; }

    public RequireTargetSide TargetSide { get; }

    public RequireTargetType TargetType { get; }

    public string GetText()
    {
        var sb = new StringBuilder();
        sb.Append($"Deal {Damage} damage");

        switch (TargetSide)
        {
            case RequireTargetSide.All:
                sb.Append(" to a random");
                break;
            case RequireTargetSide.Friendly:
                sb.Append(" to a random friendly");
                break;
            case RequireTargetSide.Enemy:
                sb.Append(" to a random enemy");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (TargetType)
        {
            case RequireTargetType.All:
                sb.Append(" creature or avatar");
                break;
            case RequireTargetType.Creature:
                sb.Append(" creature");
                break;
            case RequireTargetType.Avatar:
                sb.Append(" avatar");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return sb.ToString();
    }

    public Task ExecuteAsync(CardGameState card, PlayerGameState playerGameState)
    {
        var cardDto = card.AsDto();
        playerGameState.Nodes.EnemyCardTempArea.TmpShowCard(playerGameState.EnemyPeerId, cardDto);
        var board = playerGameState.Board.ToArray();
        var enemyBoard = playerGameState.Enemy.Board.ToArray();

        for (var i = 0; i < Count; i++)
        {
            switch (TargetType)
            {
                case RequireTargetType.All:
                    DealDamageToRandomAvatarOrCreature(playerGameState, board, enemyBoard);
                    break;
                case RequireTargetType.Creature:
                    DealDamageToRandomCreature(playerGameState, board, enemyBoard);
                    break;
                case RequireTargetType.Avatar:
                    DealDamageToRandomAvatar(playerGameState);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return Task.CompletedTask;
    }

    private void DealDamageToRandomAvatarOrCreature(PlayerGameState playerGameState,
        CardGameState[] board,
        CardGameState[] enemyBoard)
    {
        var isTargetAvatar = board.Any() && enemyBoard.Any() && Random.Shared.Next(0, 1) == 0;

        if (isTargetAvatar)
        {
            DealDamageToRandomAvatar(playerGameState);
        }
        else
        {
            DealDamageToRandomCreature(playerGameState, board, enemyBoard);
        }
    }

    private void DealDamageToRandomAvatar(PlayerGameState playerGameState)
    {
        switch (TargetSide)
        {
            case RequireTargetSide.All:
                if (Random.Shared.Next(0, 1) == 0)
                {
                    DealDamageToEnemy(playerGameState);
                }
                else
                {
                    DealDamageToSelf(playerGameState);
                }

                break;
            case RequireTargetSide.Friendly:
                DealDamageToSelf(playerGameState);
                break;
            case RequireTargetSide.Enemy:
                DealDamageToEnemy(playerGameState);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DealDamageToEnemy(PlayerGameState playerGameState)
    {
        playerGameState.Enemy.Health -= Damage;

        playerGameState.Nodes.EnemyStatusPanel.SetHealth(playerGameState.PeerId, playerGameState.Enemy.Health);
        playerGameState.Nodes.StatusPanel.SetHealth(playerGameState.EnemyPeerId, playerGameState.Enemy.Health);
    }

    private void DealDamageToSelf(PlayerGameState playerGameState)
    {
        playerGameState.Health -= Damage;

        playerGameState.Nodes.StatusPanel.SetHealth(playerGameState.PeerId, playerGameState.Enemy.Health);
        playerGameState.Nodes.EnemyStatusPanel.SetHealth(playerGameState.EnemyPeerId,
            playerGameState.Enemy.Health);
    }

    private void DealDamageToRandomCreature(PlayerGameState playerGameState,
        CardGameState[] board,
        CardGameState[] enemyBoard)
    {
        switch (TargetSide)
        {
            case RequireTargetSide.All:
                DealDamageToRandomEnemyOrFriendlyCreature(playerGameState, board, enemyBoard);
                break;
            case RequireTargetSide.Friendly:
                DealDamageToARandomCreature(playerGameState, board, PlayerGameState.ControllingEntity.Self);
                break;
            case RequireTargetSide.Enemy:
                DealDamageToARandomCreature(playerGameState, enemyBoard, PlayerGameState.ControllingEntity.Enemy);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DealDamageToRandomEnemyOrFriendlyCreature(PlayerGameState playerGameState,
        CardGameState[] board,
        CardGameState[] enemyBoard)
    {
        var isTargetSelf = board.Any() && enemyBoard.Any()
            ? Random.Shared.Next(0, 1) == 1
            : board.Any();

        if (isTargetSelf)
        {
            DealDamageToARandomCreature(playerGameState, board, PlayerGameState.ControllingEntity.Self);
        }
        else
        {
            DealDamageToARandomCreature(playerGameState, enemyBoard, PlayerGameState.ControllingEntity.Enemy);
        }
    }

    private void DealDamageToARandomCreature(PlayerGameState playerGameState, CardGameState[] board,
        PlayerGameState.ControllingEntity controllingEntity)
    {
        var idx = Random.Shared.Next(0, board.Length - 1);
        var targetCard = board[idx];
        playerGameState.ResolveDamage(targetCard, Damage, controllingEntity);
    }

    public static CardEffectRecord MakeRecord(int damage, RequireTargetSide side, RequireTargetType type, int count)
    {
        return new CardEffectRecord(Id, JsonSerializer.Serialize(new Init(damage, side, type, count)));
    }

    private record Init(int Damage, RequireTargetSide Side, RequireTargetType Type, int Count);
}