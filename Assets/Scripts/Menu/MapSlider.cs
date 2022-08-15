using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    public class MapSlider : Slider
    {
        protected MapSlider() : base()
        { }

        public float stepMultiplier = 0.01f;

        // Size of each step.
        float stepSize { get { return wholeNumbers ? 1 : (maxValue - minValue) * stepMultiplier; } }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }
        Axis axis { get { return (direction == Direction.LeftToRight || direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return direction == Direction.RightToLeft || direction == Direction.TopToBottom; } }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(reverseValue ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(reverseValue ? value - stepSize : value + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(reverseValue ? value - stepSize : value + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Set(reverseValue ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
            }
        }
    }
}