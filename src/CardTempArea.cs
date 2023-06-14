using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using OpenCCG.Core;
using OpenCCG.Net;
using OpenCCG.Net.Dto;
using OpenCCG.Net.Rpc;
using OpenCCG.Net.ServerNodes;

namespace OpenCCG;

public partial class CardTempArea : Sprite2D, IMessageReceiver<MessageType>
{
    [Export] private InputEventSystem _inputEventSystem;
    [Export] private CardStatPanel _costPanel;
    [Export] private CardInfoPanel _descriptionPanel, _namePanel;

    private TaskCompletionSource<RequireTargetOutputDto>? _tsc;
    private RequireTargetInputDto? _currentInputDto;

    private void Show(CardGameStateDto cardGameStateDto)
    {
        Visible = true;
        Texture = GD.Load<Texture2D>(cardGameStateDto.Record.ImgPath);
        _costPanel.Value = cardGameStateDto.Cost;
        _descriptionPanel.Value = cardGameStateDto.Record.Description;
        _namePanel.Value = cardGameStateDto.Record.Name;
    }

    private void Reset()
    {
        Texture = null;
        Visible = false;
        _currentInputDto = null;
    }

    private async Task<RequireTargetOutputDto> RequireTarget(RequireTargetInputDto input)
    {
        Show(input.Card);
        _tsc = new TaskCompletionSource<RequireTargetOutputDto>();
        _currentInputDto = input;
        _inputEventSystem.OnRequireTarget();

        return await _tsc.Task;
    }

    private async Task TmpShowTarget(CardGameStateDto cardGameStateDto)
    {
        Show(cardGameStateDto);
        await Task.Delay(TimeSpan.FromSeconds(3));
        Reset();
    }

    public bool TryUpstreamTarget<T>(T target)
    {
        if (_tsc is null || _tsc.Task.IsCompleted || _tsc.Task.IsCanceled || _tsc.Task.IsCanceled ||
            _tsc.Task.IsFaulted)
        {
            Logger.Error<CardTempArea>($"No request id set to use {nameof(TryUpstreamTarget)}");
            return false;
        }

        if (target is CardBoard cardBoard && _currentInputDto!.Type != RequireTargetType.Avatar)
        {
            var isEnemyBoard = cardBoard.GetParent<BoardArea>().IsEnemy;
            if ((isEnemyBoard && _currentInputDto.Side == RequireTargetSide.Friendly) ||
                (!isEnemyBoard && _currentInputDto.Side == RequireTargetSide.Enemy))
            {
                return false;
            }

            Reset();
            return _tsc.TrySetResult(new RequireTargetOutputDto(cardBoard.CardGameState.Id, null));
        }

        if (target is Avatar &&
            _currentInputDto!.Type != RequireTargetType.Creature &&
            _currentInputDto!.Side != RequireTargetSide.Enemy)
        {
            Reset();
            return _tsc.TrySetResult(new RequireTargetOutputDto(null, false));
        }

        if (target is EnemyAvatar &&
            _currentInputDto!.Type != RequireTargetType.Creature &&
            _currentInputDto.Side != RequireTargetSide.Friendly)
        {
            Reset();
            return _tsc.TrySetResult(new RequireTargetOutputDto(null, true));
        }

        return false;
    }

    public Dictionary<string, IObserver>? Observers => null;

    [Rpc]
    public async void HandleMessageAsync(string messageJson)
    {
        await IMessageReceiver<MessageType>.HandleMessageAsync(this, messageJson);
    }

    public Executor GetExecutor(MessageType messageType) => messageType switch
    {
        MessageType.RequireTarget => Executor.Make<RequireTargetInputDto, RequireTargetOutputDto>(RequireTarget),
        MessageType.TmpShowCard => Executor.Make<CardGameStateDto>(TmpShowTarget)
    };
}