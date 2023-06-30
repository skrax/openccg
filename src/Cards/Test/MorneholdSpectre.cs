using System;
using System.Linq;
using System.Threading.Tasks;
using OpenCCG.Net;

namespace OpenCCG.Cards.Test;

public class MorneholdSpectre : CreatureImplementation
{
    public MorneholdSpectre(ICreatureOutline outline, PlayerGameState2 playerGameState) :
        base(outline, new(), playerGameState)
    {
    }

    public override async Task OnExitAsync()
    {
        if (!PlayerGameState.Enemy.Board.Any()) return;

        var board = PlayerGameState.Enemy.Board;

        var idx = Random.Shared.Next(0, board.Count - 1);
        var target = board.ElementAt(idx);

        // await PlayerGameState.ResolveDamageAsync(target, 3);
    }
}