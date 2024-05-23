﻿using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using System.Text;

namespace SAIN.Layers
{
    public abstract class SAINLayer : CustomLayer
    {
        public static string BuildLayerName<T>()
        {
            return $"{nameof(SAIN)} {typeof(T).Name}";
        }

        public SAINLayer(BotOwner botOwner, int priority, string layerName) : base(botOwner, priority)
        {
            LayerName = layerName;
        }

        private readonly string LayerName;

        public override string GetName() => LayerName;

        public SAINBotController BotController => SAINPlugin.BotController;
        public DecisionWrapper Decisions => SAIN?.Memory?.Decisions;

        private Bot _SAIN = null;
        public Bot SAIN
        {
            get
            {
                if (_SAIN == null && BotOwner?.BotState == EBotState.Active)
                {
                    _SAIN = BotOwner.GetComponent<Bot>();
                }

                return _SAIN;
            }
        }
        

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            if (SAIN != null)
            {
                DebugOverlay.AddBaseInfo(SAIN, BotOwner, stringBuilder);
            }
        }
    }
}