﻿using UnityEngine;

namespace Stairs
{
    public class MyTransitionLayer : TransitionDriver.OverrideLayer
    {
        public MyTransitionLayer(Navigator navigator) : base(navigator)
        {
			isMovingOnStaris = false;
        }
        public override void BeginTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            base.BeginTransition(navigator, transition);
			if (transition.y != 1 && transition.y != -1) return;
			if (transition.x != 1 && transition.x != -1) return;
			if (transition.start != NavType.Floor || transition.end != NavType.Floor) return;
			int cell = Grid.PosToCell(navigator);
			int offset;
			if (transition.y > 0)
			{
				offset = Grid.OffsetCell(cell, transition.x, 0);
				if (transition.x > 0)
				{
					if (MyGrid.IsRightSet(offset)) return;
				}
				else
				{
					if (!MyGrid.IsRightSet(offset)) return;
				}
			}
			else
			{
				offset = Grid.OffsetCell(cell, 0, transition.y);
				if (transition.x > 0)
				{
					if (!MyGrid.IsRightSet(offset)) return;
				}
				else
				{
					if (MyGrid.IsRightSet(offset)) return;
				}
			}
			if (!MyGrid.IsWalkable(offset)) return;


			if (transition.y > 0)
			{
				transition.speed *= 0.9f * cos45;
				transition.animSpeed *= 0.9f * cos45;
			}
			else
			{
				transition.speed *= 1.5f * cos45;
				transition.animSpeed *= 1.5f * cos45;
			}

			transition.isLooping = true;
			transition.anim = "floor_floor_1_0_loop";
			isMovingOnStaris =true;
			time = Time.timeSinceLevelLoad;
			int target_cell = Grid.OffsetCell(cell, transition.x, transition.y);
			targetPos = Grid.CellToPosCBC(target_cell, navigator.sceneLayer);
			startPos = navigator.transform.GetPosition();
		}
		public override void UpdateTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            base.UpdateTransition(navigator, transition);
			if (!isMovingOnStaris) return;
			transition.isLooping = false;
			Vector3 pos = navigator.transform.GetPosition();
			float step = (Time.timeSinceLevelLoad - time) * transition.speed;
			time = Time.timeSinceLevelLoad;

			if (transition.y > 0)
			{
				pos.y += step;
			}
			else if (transition.y < 0)
			{
				pos.y -= step;
			}
			if (transition.x > 0)
			{
				pos.x += step;
			}
			else if (transition.x < 0)
			{
				pos.x -= step;
			}

			if ((transition.y > 0 && pos.y > targetPos.y) || (transition.y < 0 && pos.y < targetPos.y))
			{
                transition.isLooping = true;
                return;
			}

			navigator.transform.position = pos;
		}

		public override void EndTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            base.EndTransition(navigator, transition);
            if (isMovingOnStaris)
            {
				if (!transition.isLooping)
                {
					navigator.transform.position = startPos;
                }
            }
			isMovingOnStaris = false;
        }
        public bool isMovingOnStaris;
		private float time;
		private Vector3 startPos;
		private Vector3 targetPos;
		public static readonly float cos45 = 0.7071f;
	}
}