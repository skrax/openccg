using System.Threading.Tasks;
using OpenCCG.Net;

namespace OpenCCG.Cards;

public abstract class SpellImplementation : CardImplementation
{
    protected SpellImplementation(SpellOutline outline, PlayerGameState playerGameState) :
        base(outline, playerGameState,
            new SpellState
            {
                Cost = outline.Cost,
                Zone = CardZone.Deck
            })
    {
    }

    public SpellOutline SpellOutline => (SpellOutline)Outline;

    public SpellState SpellState => (SpellState)State;

    public abstract override Task OnPlayAsync();

    public override CardImplementationDto AsDto()
    {
        return CardImplementationDto.AsSpell(Id, SpellOutline, SpellState);
    }
}