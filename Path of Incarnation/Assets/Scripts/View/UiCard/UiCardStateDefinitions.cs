using DG.Tweening;
using UnityEngine;
using State = StateMachine<UiCard>.State;

/// <summary>
/// All state classes for UiCard state machine.
/// Each state represents a distinct UI state (idle, hovering, dragging, etc.)
/// </summary>
public static class UiCardStateDefinitions
{
    #region Slot States (Board)

    public class StateInSlotIdle : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.ResetToBaseScale();
        }
    }

    public class StateInSlotHovering : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.ApplyHoverScale();
        }
    }

    public class StateInSlotPressed : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.SnapToMousePosition();
        }
    }

    public class StateDraggingFromSlot : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.StartDraggingVisuals();
        }

        protected override void OnUpdate()
        {
            Owner.SnapToMousePosition();
        }

        protected override void OnExit(State nextState)
        {
            Owner.EndDraggingVisuals();
        }
    }

    #endregion

    #region Hand States

    public class StateInHandIdle : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.ResetToBaseScale();
            Owner.ResetVisualPosition();
        }
    }

    public class StateInHandHover : State
    {
        private Vector2 baseAnchoredPos;
        private HandSplineLayout layout;

        protected override void OnEnter(State prevState)
        {
            Owner.ApplyHoverScale();

            layout = Owner.RectTransform?.GetComponentInParent<HandSplineLayout>();
            layout?.ApplyPyramidZOrder(Owner);

            if (Owner.VisualTransform)
            {
                baseAnchoredPos = Vector2.zero;
                Owner.LiftVisual(baseAnchoredPos);
            }
        }

        protected override void OnExit(State nextState)
        {
            if (Owner.VisualTransform)
            {
                Owner.LowerVisual(baseAnchoredPos);
            }

            // Reset z-order if not dragging
            if (!(nextState is StateDraggingFromHand))
            {
                layout?.ApplyPyramidZOrder(null);
            }

            Owner.ResetToBaseScale();
        }
    }

    public class StateInHandPressed : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.SnapToMousePosition();
        }
    }

    public class StateDraggingFromHand : State
    {
        private int savedSiblingIndex;

        protected override void OnEnter(State prevState)
        {
            savedSiblingIndex = Owner.RectTransform.GetSiblingIndex();
            Owner.StartDraggingVisuals();
        }

        protected override void OnUpdate()
        {
            Owner.SnapToMousePosition();
        }

        protected override void OnExit(State nextState)
        {
            Owner.RectTransform.SetSiblingIndex(savedSiblingIndex);
            Owner.EndDraggingVisuals();
        }
    }

    #endregion

    #region Transition State

    public class StateMovingToSlot : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.AnimateToCurrentZone();
        }
    }

    #endregion
}