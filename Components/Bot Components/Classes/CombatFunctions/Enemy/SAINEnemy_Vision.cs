﻿using BepInEx.Logging;
using EFT;
using SAIN.Components;
using static SAIN.UserSettings.VisionConfig;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

namespace SAIN.Classes
{
    public class SAINEnemy_Vision : SAINBot
    {
        public Dictionary<BodyPartClass, GClass478> ActiveParts { get; private set; }
        public GClass475 GoalEnemy { get; private set; }
        public Player EnemyPlayer { get; private set; }
        public EnemyComponent EnemyComponent { get; private set; }

        private readonly float BotDifficultyModifier;

        public SAINEnemy_Vision(BotOwner bot, IAIDetails person, float BotDifficultyMod) : base(bot)
        {
            GoalEnemy = bot.Memory.GoalEnemy;
            Person = person;
            EnemyPlayer = person.GetPlayer;
            BotDifficultyModifier = BotDifficultyMod;
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            ActiveParts = bot.Memory.GoalEnemy.AllActiveParts;
             
            if (SAIN.Squad.BotInGroup)
            {
                FindEnemyComponent();
            }
        }

        private void FindEnemyComponent()
        {
            if (EnemyPlayer == null)
            {
                return;
            }
            var componentArray = EnemyPlayer.gameObject.GetComponents<EnemyComponent>();
            if (componentArray == null || componentArray.Length == 0)
            {
                AddNewComponent();
            }
            else
            {
                if (componentArray != null)
                {
                    foreach (var component in componentArray)
                    {
                        if (component != null && component.SquadId == SAIN.SquadId)
                        {
                            EnemyComponent = component;
                            break;
                        }
                    }
                }
            }
            if (EnemyComponent == null)
            {
                AddNewComponent();
            }
        }

        private void AddNewComponent()
        {
            Logger.LogDebug($"New Enemy Component added for enemy: [{EnemyPlayer.name}] for SquadID: [{SAIN.SquadId}]");
            EnemyComponent = EnemyPlayer.gameObject.AddComponent<EnemyComponent>();
            EnemyComponent.Init(EnemyPlayer, SAIN.SquadId, SAIN);
        }

        private readonly ManualLogSource Logger;

        public void Update()
        {
            if (EnemyPlayer == null || !EnemyPlayer.HealthController.IsAlive)
            {
                return;
            }
            UpdateDistance();
            UpdatePath();
        }

        private void UpdateDistance()
        {
            if (DistanceTimer < Time.time)
            {
                DistanceTimer = Time.time + 0.25f;
                float distance = GetMagnitudeToBot(Position);
                RealDistance = distance;
                LastSeenDistance = IsVisible ? distance : GetMagnitudeToBot(PositionLastSeen);
                DistanceFromLastSeen = IsVisible ? 0f : (PositionLastSeen - Position).magnitude;
            }
        }

        public float DistanceFromLastSeen { get; private set; }
        public Vector3 Position => Person.Position;

        private float GetMagnitudeToBot(Vector3 point)
        {
            return (BotOwner.Position - point).magnitude;
        }

        private bool _enemyIsReloading;
        private float _soundResetTimer;

        public bool EnemyIsReloading
        {
            get
            {
                if (_soundResetTimer < Time.time)
                {
                    _enemyIsReloading = false;
                }
                return _enemyIsReloading;
            }
            set
            {
                if (value == true)
                {
                    _enemyIsReloading = true;
                    _soundResetTimer = Time.time + 3f * Random.Range(0.75f, 1.5f);
                }
            }
        }

        private bool _enemyHasGrenade;
        private float _grenadeResetTimer;

        public bool EnemyHasGrenadeOut
        {
            get
            {
                if (_grenadeResetTimer < Time.time)
                {
                    _enemyHasGrenade = false;
                }
                return _enemyHasGrenade;
            }
            set
            {
                if (value == true)
                {
                    _enemyHasGrenade = true;
                    _grenadeResetTimer = Time.time + 3f * Random.Range(0.75f, 1.5f);
                }
            }
        }

        private bool _enemyIsHeal;
        private float _healResetTimer;

        public bool EnemyIsHealing
        {
            get
            {
                if (_healResetTimer < Time.time)
                {
                    _enemyIsHeal = false;
                }
                return _enemyIsHeal;
            }
            set
            {
                if (value == true)
                {
                    _enemyIsHeal = true;
                    _healResetTimer = Time.time + 4f * Random.Range(0.75f, 1.25f);
                }
            }
        }


        private float SoundResetTimer = 0f;

        public float RealDistance { get; private set; }
        public float LastSeenDistance { get; private set; }
        public Vector3 PositionLastSeen { get; private set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen { get; private set; }
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public bool CanHearCloseVisible { get; private set; }
        public bool EnemyClose { get; private set; }
        
        private float DistanceTimer = 0f;

        public void OnGainSight(float percentage)
        {
            if (percentage > 50f)
            {
                UpdateVisible(true);
                return;
            }
            if (IsVisible)
            {
                UpdateVisible(true);
            }
            else
            {
                bool random = EFTMath.RandomBool(25);
                UpdateVisible(random);
            }
        }

        public void OnLoseSight()
        {
            UpdateVisible(false);
        }

        public void UpdateCanShoot(bool value, float percentage)
        {
            PercentageEnemyCanShoot = percentage;
            CanShoot = value;
        }

        public float PercentageEnemyCanShoot { get; private set; }
        public Vector3 Direction => Position - BotPosition;

        private void UpdateVisible(bool inLineOfSight)
        {
            InLineOfSight = inLineOfSight;
            float realDistance = RealDistance;
            bool close = realDistance < 15f;
            var move = Person.GetPlayer.MovementContext;
            bool enemySprinting = move.IsSprintEnabled;
            float enemyMoveSpeed = move.ClampedSpeed / move.MaxSpeed;

            bool canHear = (enemySprinting && realDistance < 25f) || close && enemyMoveSpeed > 0.35f;
            bool visible = false;
            if (canHear)
            {
                visible = true;
            }
            else if (inLineOfSight)
            {
                float angle = Vector3.Angle(Direction.normalized, BotOwner.LookDirection);
                if (close && angle < 75)
                {
                    visible = true;
                }
                else if (GoalEnemy?.VisibleOnlyBySense == EEnemyPartVisibleType.visible)
                {
                    visible = true;
                }
            }

            bool wasVisible = IsVisible;
            IsVisible = visible;

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                }
                TimeSinceSeen = 0f;
                if (!Seen)
                {
                    TimeFirstSeen = Time.time;
                    Seen = true;
                }
            }
            if (!IsVisible)
            {
                VisibleStartTime = -1f;
                if (wasVisible)
                {
                    TimeLastSeen = Time.time;
                    PositionLastSeen = Person.Position;
                }
                if (Seen)
                {
                    TimeSinceSeen = Time.time - TimeLastSeen;
                }
            }

            CanHearCloseVisible = canHear;
        }

        public void UpdatePath()
        {
            if (CheckPathTimer < Time.time)
            {
                CheckPathTimer = Time.time + 0.5f;

                CalcPath(Person.Position);
            }
        }

        private void CalcPath(Vector3 pos)
        {
            Path.ClearCorners();
            if (NavMesh.CalculatePath(BotOwner.Transform.position, pos, -1, Path))
            {
                PathDistance = Path.CalculatePathLength();
            }
        }

        public SAINEnemyPath CheckPathDistance()
        {
            const float VeryCloseDist = 5f;
            const float CloseDist = 20f;
            const float FarDist = 80f;
            const float VeryFarDist = 120f;

            SAINEnemyPath pathDistance;
            float distance = PathDistance;

            if (distance <= VeryCloseDist)
            {
                pathDistance = SAINEnemyPath.VeryClose;
            }
            else if (distance <= CloseDist)
            {
                pathDistance = SAINEnemyPath.Close;
            }
            else if (distance <= FarDist)
            {
                pathDistance = SAINEnemyPath.Mid;
            }
            else if (distance <= VeryFarDist)
            {
                pathDistance = SAINEnemyPath.Far;
            }
            else
            {
                pathDistance = SAINEnemyPath.VeryFar;
            }

            return pathDistance;
        }

        public NavMeshPath Path = new NavMeshPath();
        public float PathDistance { get; private set; }

        private float CheckPathTimer = 0f;

        public bool EnemyLookingAtMe
        {
            get
            {
                Vector3 EnemyLookDirection = VectorHelpers.NormalizeFastSelf(BotOwner.LookSensor._headPoint - Person.Transform.position);
                return VectorHelpers.IsAngLessNormalized(EnemyLookDirection, Person.LookDirection, 0.9659258f);
            }
        }

        public Vector3 EnemyHeadPosition => Person.MainParts[BodyPartType.head].Position;

        public Vector3 EnemyChestPosition => Person.MainParts[BodyPartType.body].Position;

        public IAIDetails Person { get; private set; }

        public bool InLineOfSight { get; private set; }

        public bool IsVisible { get; private set; }

        public bool CanShoot { get; private set; }
    }
}