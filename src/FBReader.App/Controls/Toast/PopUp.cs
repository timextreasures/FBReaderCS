/*
 * Author: CactusSoft (http://cactussoft.biz/), 2013
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
 * 02110-1301, USA.
 */

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Clarity.Phone.Extensions;

using Microsoft.Phone.Controls;

namespace Coding4Fun.Toolkit.Controls
{
    public abstract class PopUp<T, TPopUpResult> : Control
    {
        internal DialogService PopUpService;
        private PhoneApplicationPage _startingPage;
        private bool _alreadyFired;

        public bool IsOpen { get { return PopUpService != null && PopUpService.IsOpen; } }
        public bool IsAppBarVisible { get; set; }

        // adjust for SIP
        private bool _isCalculateFrameVerticalOffset;

        protected bool IsCalculateFrameVerticalOffset
        {
            get { return _isCalculateFrameVerticalOffset; }
            set
            {
                _isCalculateFrameVerticalOffset = value;

                if (_isCalculateFrameVerticalOffset)
                {
                    var bind = new System.Windows.Data.Binding("Y");
                    var frame = (Application.Current.RootVisual as Frame);

                    if (frame != null)
                    {
                        var transGroup = frame.RenderTransform as TransformGroup;

                        if (transGroup != null)
                        {
                            bind.Source = transGroup.Children.FirstOrDefault(t => t is TranslateTransform);
                            SetBinding(FrameTransformProperty, bind);
                        }
                    }
                }
            }
        }

        public bool IsOverlayApplied
        {
            get { return _isOverlayApplied; }
            set { _isOverlayApplied = value; }
        }
        private bool _isOverlayApplied = true;

        internal bool IsSetAppBarVisibiilty { get; set; }
        internal TimeSpan MainBodyDelay { get; set; }

        protected internal bool IsBackKeyOverride { get; set; }
        protected DialogService.AnimationTypes AnimationType { get; set; }

        public event EventHandler<PopUpEventArgs<T, TPopUpResult>> Completed;
        public event EventHandler Opened;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (PopUpService != null)
            {
                PopUpService.BackgroundBrush = Overlay;

                PopUpService.ApplyOverlayBackground();
                PopUpService.SetAlignmentsOnOverlay(HorizontalAlignment, VerticalAlignment);
            }
        }

        public virtual void OnCompleted(PopUpEventArgs<T, TPopUpResult> result)
        {
            _alreadyFired = true;

            if (Completed != null)
                Completed(this, result);

            if (PopUpService != null)
                PopUpService.Hide();

            if (PopUpService != null && PopUpService.BackButtonPressed)
                ResetWorldAndDestroyPopUp();
        }

        static void OnFrameTransformPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var sender = source as PopUp<T, TPopUpResult>;

            if (sender == null || sender.PopUpService == null)
                return;

            if (!sender.IsCalculateFrameVerticalOffset)
                return;

            sender.PopUpService.ControlVerticalOffset = -sender.FrameTransform;
            sender.PopUpService.CalculateVerticalOffset();
        }

        public virtual void Show()
        {
            if (IsOpen)
                return;

            if (PopUpService == null)
            {
                PopUpService = new DialogService
                {
                    AnimationType = AnimationType,
                    Child = this,
                    IsBackKeyOverride = IsBackKeyOverride,
                    IsOverlayApplied = IsOverlayApplied,
                    MainBodyDelay = MainBodyDelay,
                };
            }


            // this will happen if the user comes in OnNavigate or 
            // something where the DOM hasn't been created yet.
            if (PopUpService.Page == null)
            {
                Dispatcher.BeginInvoke(Show);

                return;
            }

            if (IsCalculateFrameVerticalOffset)
            {
                PopUpService.ControlVerticalOffset = -FrameTransform;
            }

            PopUpService.Closed += PopUpClosed;
            PopUpService.Opened += PopUpOpened;


            if (!IsAppBarVisible && PopUpService.Page.ApplicationBar != null && PopUpService.Page.ApplicationBar.IsVisible)
            {
                PopUpService.Page.ApplicationBar.IsVisible = false;

                IsSetAppBarVisibiilty = true;
            }

            _startingPage = PopUpService.Page;

            PopUpService.Show();
        }

        void PopUpOpened(object sender, EventArgs e)
        {
            if (Opened != null)
                Opened(sender, e);
        }

        protected virtual TPopUpResult GetOnClosedValue()
        {
            return default(TPopUpResult);
        }

        public void Hide()
        {
            PopUpClosed(this, null);
        }

        void PopUpClosed(object sender, EventArgs e)
        {
            if (!_alreadyFired)
            {
                OnCompleted(new PopUpEventArgs<T, TPopUpResult> { PopUpResult = GetOnClosedValue() });
                return;
            }

            ResetWorldAndDestroyPopUp();
        }

        private void ResetWorldAndDestroyPopUp()
        {
            if (PopUpService != null)
            {
                if (!IsAppBarVisible && IsSetAppBarVisibiilty)
                {
                    _startingPage.ApplicationBar.IsVisible = IsSetAppBarVisibiilty;
                }

                _startingPage = null;

                PopUpService.Child = null;
                PopUpService = null;
            }
        }

        double FrameTransform
        {
            get { return (double)GetValue(FrameTransformProperty); }
            set { SetValue(FrameTransformProperty, value); }
        }

        static readonly DependencyProperty FrameTransformProperty = DependencyProperty.Register(
              "FrameTransform",
              typeof(double),
              typeof(PopUp<T, TPopUpResult>),
              new PropertyMetadata(0.0, OnFrameTransformPropertyChanged));

        public Brush Overlay
        {
            get { return (Brush)GetValue(OverlayProperty); }
            set { SetValue(OverlayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Overlay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverlayProperty =
            DependencyProperty.Register("Overlay", typeof(Brush), typeof(PopUp<T, TPopUpResult>), new PropertyMetadata(new SolidColorBrush()));
    }
}