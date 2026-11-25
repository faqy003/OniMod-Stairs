using KSerialization;
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
            //isMovingOnStaris = true;
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
                // 向上：看同高度侧边那格是不是楼梯的"右接"侧
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
                // 向下：看正下方那格是不是楼梯的"右接"侧
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
            transitionOriginCell = cell;
            transitionHypotenuseCell = offset;
            transitionTargetCell = Grid.OffsetCell(cell, transition.x, transition.y);
            targetPos = Grid.CellToPosCBC(transitionTargetCell, navigator.sceneLayer);
            startPos = navigator.transform.GetPosition();
        }

        public override void EndTransition(Navigator navigator, Navigator.ActiveTransition transition)
        {
            base.EndTransition(navigator, transition);

            if (isMovingOnStaris)
            {
                int cell = Grid.PosToCell(navigator.transform.GetPosition());
                if (MyGrid.IsHypotenuse(cell))
                {
                    if (pendingStop && Grid.IsValidCell(transitionTargetCell))
                    {
                        navigator.transform.position = targetPos;
                    }
                    else
                    {
                        navigator.transform.position = startPos;
                    }
                }
            }
            isMovingOnStaris = false;
            transitionOriginCell = Grid.InvalidCell;
            transitionHypotenuseCell = Grid.InvalidCell;
            transitionTargetCell = Grid.InvalidCell;

        }

        public bool isMovingOnStaris = false;
        public int transitionOriginCell = Grid.InvalidCell;
        public bool allowForcedStop = false;
        private int transitionHypotenuseCell = Grid.InvalidCell;
        private int transitionTargetCell = Grid.InvalidCell;
        private bool pendingStop;
        private bool pendingStopArrived;
        private bool pendingStopPlayIdle;
        private Vector3 startPos;
        private Vector3 targetPos;

        public bool DeferStop(Navigator navigator, bool arrivedAtDestination, bool playIdle)
        {
            if (!isMovingOnStaris) return false;
            if (allowForcedStop)
            {
                pendingStop = false;
                return false;
            }

            int currentCell = Grid.PosToCell(navigator);
            bool invalidCell = !Grid.IsValidCell(currentCell);
            bool supportRemoved = MyGrid.Masks == null ||
                !Grid.IsValidCell(transitionHypotenuseCell) ||
                !MyGrid.IsHypotenuse(transitionHypotenuseCell);
            bool navTypeChanged = navigator.CurrentNavType != NavType.Floor;
            if (invalidCell || supportRemoved || navTypeChanged)
            {
                pendingStop = false;
                return false;
            }

            pendingStop = true;
            pendingStopArrived = arrivedAtDestination;
            pendingStopPlayIdle = playIdle;
            return true;
        }

        public bool TryTakePendingStop(out bool arrivedAtDestination, out bool playIdle)
        {
            arrivedAtDestination = pendingStopArrived;
            playIdle = pendingStopPlayIdle;
            if (isMovingOnStaris || !pendingStop) return false;

            pendingStop = false;
            return true;
        }
    }
}
