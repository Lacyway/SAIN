﻿using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
	public class EnemyAnglesClass : EnemyBase, IBotEnemyClass
	{
		private const float CALC_ANGLE_FREQ = 1f / 15f;
		private const float CALC_ANGLE_FREQ_AI = 1f / 4f;
		private const float CALC_ANGLE_FREQ_KNOWN = 1f / 30f;
		private const float CALC_ANGLE_FREQ_KNOWN_AI = 1f / 15f;
		private const float CALC_ANGLE_CURRENT_COEF = 0.5f;

		public bool CanBeSeen { get; private set; }
		public float MaxVisionAngle { get; private set; }
		public float AngleToEnemy { get; private set; }
		public float AngleToEnemyHorizontal { get; private set; }
		public float AngleToEnemyHorizontalSigned { get; private set; }
		public float AngleToEnemyVertical { get; private set; }
		public float AngleToEnemyVerticalSigned { get; private set; }

		private float _calcAngleTime;
		private TimeClass _globalTimeClass;

		public EnemyAnglesClass(Enemy enemy) : base(enemy)
		{
		}

		public void Init()
		{
			_globalTimeClass = SAINBotController.Instance.TimeVision;
		}

		public void Update()
		{
			checkCalculateByFrequency();
		}

		public void Dispose()
		{ }

		public void OnEnemyKnownChanged(bool known, Enemy enemy)
		{ }

		private void checkCalculateByFrequency()
		{
			if (_calcAngleTime < Time.time)
			{
				_calcAngleTime = Time.time + getDelay();
				calculateAngles();
			}
		}

		private float getDelay()
		{
			float delay;
			if (Enemy.IsAI)
				delay = Enemy.EnemyKnown ? CALC_ANGLE_FREQ_KNOWN_AI : CALC_ANGLE_FREQ_AI;
			else
				delay = Enemy.EnemyKnown ? CALC_ANGLE_FREQ_KNOWN : CALC_ANGLE_FREQ;

			if (Enemy.IsCurrentEnemy)
				delay *= CALC_ANGLE_CURRENT_COEF;
			return delay;
		}

		private void calculateAngles()
		{
			MaxVisionAngle = calcVisionAngle();

			Vector3 lookDir = Bot.LookDirection;
			Vector3 enemyDirNormal = Enemy.EnemyDirectionNormal;

			AngleToEnemy = Vector3.Angle(enemyDirNormal, lookDir);
			CanBeSeen = AngleToEnemy <= MaxVisionAngle;

			float verticalAngle = calcVerticalAngle(enemyDirNormal, lookDir, out float yDiff);
			AngleToEnemyVertical = verticalAngle;
			AngleToEnemyVerticalSigned = yDiff >= 0 ? verticalAngle : -verticalAngle;

			float horizSigned = calcHorizontalAngle(enemyDirNormal, lookDir);
			AngleToEnemyHorizontalSigned = horizSigned;
			AngleToEnemyHorizontal = Mathf.Abs(horizSigned);
		}

		private float calcVisionAngle()
		{
			var fileSettings = Bot.Info.FileSettings;

			float botSetting = fileSettings.Core.VisibleAngle / 2f;
			float globalMax = _globalTimeClass.Settings.MaxFieldOfViewAngle / 2f;
			if (globalMax >= botSetting)
			{
				return botSetting;
			}

			float min;
			if (BotOwner.NightVision.UsingNow)
			{
				min = fileSettings.Look.Visible_Angle_NVGs;
			}
			else if (BotOwner.BotLight.IsEnable && PlayerComponent.Flashlight.WhiteLight)
			{
				min = fileSettings.Look.Visible_Angle_Flashlight;
			}
			else
			{
				min = GlobalSettingsClass.Instance.Look.VisionCone.Visible_Angle_Minimum;
			}

			return Mathf.Clamp(globalMax, min, 180);
		}

		private float calcVerticalAngle(Vector3 enemyDirNormal, Vector3 lookDirection, out float yDiff)
		{
			Vector3 enemyElevDir = new Vector3(lookDirection.x, enemyDirNormal.y, lookDirection.z);
			yDiff = (enemyElevDir.y - lookDirection.y).Round100();
			if (yDiff == 0)
			{
				return 0;
			}
			float angle = Vector3.Angle(lookDirection, enemyElevDir);
			return angle;
		}

		private float calcHorizontalAngle(Vector3 enemyDirNormal, Vector3 lookDirection)
		{
			enemyDirNormal.y = 0;
			lookDirection.y = 0;
			float signedAngle = Vector3.SignedAngle(lookDirection, enemyDirNormal, Vector3.up);
			return signedAngle;
		}
	}
}