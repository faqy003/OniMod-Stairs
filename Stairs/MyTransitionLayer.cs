using UnityEngine;

namespace Stairs
{
    public class MyTransitionLayer : TransitionDriver.OverrideLayer
    {
        public const float cos45 = 0.7071f;
        public const float upwardsMovementSpeedMultiplier = 0.9f;
        public const float downwardsMovementSpeedMultiplier = 1.5f;
        public const float scaffoldingSpeedMultiplier = 1.5f;
        public MyTransitionLayer(Navigator navigator) : base(navigator)
        {
            isMovingOnStaris = false;
            time = Time.time;
        }
        public override void BeginTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            base.BeginTransition(navigator, transition);
            if (transition.start != NavType.Floor || transition.end != NavType.Floor) return;

            int cell = Grid.PosToCell(navigator);
            if (transition.y == 0 && transition.x != 0) //scaffolding_alt2
            {
                if (!MyGrid.IsScaffolding(cell)) return;
                int cell_below = Grid.CellBelow(cell);
                if (Grid.Foundation[cell_below]) return;
                GameObject go = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
                if (go == null) return;
                if (go.PrefabID() != Patches.tag_ScaffoldingAlt2) return;
                transition.speed *= scaffoldingSpeedMultiplier;
                transition.animSpeed *= scaffoldingSpeedMultiplier;
                return;
            }

            if (transition.y != 1 && transition.y != -1) return;
            if (transition.x != 1 && transition.x != -1) return;
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
            if (!MyGrid.IsHypotenuse(offset)) return;

            if (transition.y > 0)
            {
                transition.speed *= upwardsMovementSpeedMultiplier * cos45;
                transition.animSpeed *= upwardsMovementSpeedMultiplier * cos45;
            }
            else
            {
                transition.speed *= downwardsMovementSpeedMultiplier * cos45;
                transition.animSpeed *= downwardsMovementSpeedMultiplier * cos45;
            }

            transition.isLooping = true;
            transition.anim = "floor_floor_1_0_loop";
            isMovingOnStaris = true;
            time = Time.time;
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
            float step = (Time.time - time) * transition.speed;
            time = Time.time;

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
    }
}
