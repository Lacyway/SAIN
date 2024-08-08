﻿using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPartsClass : EnemyBase
    {
        public bool LineOfSight => TimeSinceInLineOfSight < LINEOFSIGHT_TIME;
        public float TimeSinceInLineOfSight => Time.time - _timeLastInSight;
        public Vector3 LastSuccessLookPosition { get; private set; }
        public bool CanShoot => TimeSinceCanShoot < CANSHOOT_TIME;
        public float TimeSinceCanShoot => Time.time - _timeLastCanShoot;
        public Vector3 LastSuccessShootPosition { get; private set; }
        public Dictionary<EBodyPart, EnemyPartDataClass> Parts { get; } = new Dictionary<EBodyPart, EnemyPartDataClass>();
        public EnemyPartDataClass[] PartsArray { get; private set; }

        private float _timeLastInSight;
        private float _timeLastCanShoot;
        private int _index;
        private readonly int _indexMax;

        private const float LINEOFSIGHT_TIME = 0.1f;
        private const float CANSHOOT_TIME = 0.1f;

        public EnemyPartsClass(Enemy enemy) : base(enemy)
        {
            createPartDatas(enemy.Player.PlayerBones);
            PartsArray = Parts.Values.ToArray();
            _indexMax = Parts.Count;
        }

        public void Update()
        {
            updateParts();
        }

        private void updateParts()
        {
            bool inSight = false;
            bool canShoot = false;
            float time = Time.time;

            foreach (var part in Parts.Values) {
                part.Update(Enemy);

                if (!canShoot && part.CanShoot) {
                    canShoot = true;
                    _timeLastCanShoot = time;
                }

                if (!inSight && part.LineOfSight) {
                    inSight = true;
                    _timeLastInSight = time;
                }
            }
        }

        public EnemyPartDataClass GetNextPart()
        {
            EnemyPartDataClass result = null;
            EBodyPart epart = (EBodyPart)_index;
            if (!Parts.TryGetValue(epart, out result)) {
                _index = 0;
                result = Parts[EBodyPart.Chest];
            }

            _index++;
            if (_index > _indexMax) {
                _index = 0;
            }

            if (result == null) {
                result = Parts.PickRandom().Value;
            }
            return result;
        }

        private void createPartDatas(PlayerBones bones)
        {
            var parts = Enemy.EnemyPlayerComponent.BodyParts.Parts;
            foreach (var bodyPart in parts) {
                Parts.Add(bodyPart.Key, new EnemyPartDataClass(bodyPart.Key, bodyPart.Value.Transform, bodyPart.Value.Colliders));
            }
        }

    }
}