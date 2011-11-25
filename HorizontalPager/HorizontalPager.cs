/*
 * Copyright (C) 2007 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Cheesebaron.HorizontalPager
{
    public delegate void ScreenChangedEventHandler(object sender, EventArgs e);

    class HorizontalPager : ViewGroup
    {
        /*
         * How long to animate between screens when programmatically setting with setCurrentScreen using
         * the animate parameter
         */
        private const int ANIMATION_SCREEN_DURATION_MILLIS = 500;
        // What fraction (1/x) of the screen the user must swipe to indicate a page change
        private const int FRACTION_OF_SCREEN_WIDTH_FOR_SWIPE = 4;
        private const int INVALID_SCREEN = -1;
        /*
         * Velocity of a swipe (in density-independent pixels per second) to force a swipe to the
         * next/previous screen. Adjusted into mDensityAdjustedSnapVelocity on init.
         */
        private const int SNAP_VELOCITY_DIP_PER_SECOND = 600;
        // Argument to getVelocity for units to give pixels per second (1 = pixels per millisecond).
        private const int VELOCITY_UNIT_PIXELS_PER_SECOND = 1000;

        private const int TOUCH_STATE_REST = 0;
        private const int TOUCH_STATE_HORIZONTAL_SCROLLING = 1;
        private const int TOUCH_STATE_VERTICAL_SCROLLING = -1;

        private int mCurrentScreen;
        private int mDensityAdjustedSnapVelocity;
        private bool mFirstLayout = true;
        private float mLastMotionX;
        private float mLastMotionY;
        private int mMaximumVelocity;
        private int mNextScreen = INVALID_SCREEN;
        private Scroller mScroller;
        private int mTouchSlop;
        private int mTouchState = TOUCH_STATE_REST;
        private VelocityTracker mVelocityTracker;
        private int mLastSeenLayoutWidth = -1;
        private Display mDisplay;

        public int CurrentScreen
        {
            get { return mCurrentScreen; }
        }

        public event ScreenChangedEventHandler ScreenChanged;

        protected virtual void OnChanged(EventArgs e)
        {
            if (ScreenChanged != null)
                ScreenChanged(this, e);
        }

        public HorizontalPager(Context context)
            : base(context)
        {
            Init(null);
        }

        public HorizontalPager(Context context, Display display)
            : base(context)
        {
            mDisplay = display;
            Init(mDisplay);
        }

        public HorizontalPager(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init(null);
        }

        private void Init(Display display)
        {
            mScroller = new Scroller(Context);
            DisplayMetrics displayMetrics = new DisplayMetrics();

            if (display != null)
                display.GetMetrics(displayMetrics);
            else
            {
                throw new Exception("Could not get Display Metrics");
            }

            mDensityAdjustedSnapVelocity = (int)(displayMetrics.Density * SNAP_VELOCITY_DIP_PER_SECOND);

            ViewConfiguration configuration = ViewConfiguration.Get(Context);
            mTouchSlop = configuration.ScaledTouchSlop;
            mMaximumVelocity = configuration.ScaledMaximumFlingVelocity;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            int width = MeasureSpec.GetSize(widthMeasureSpec);
            MeasureSpecMode widthMode = MeasureSpec.GetMode(widthMeasureSpec);
            if (widthMode != MeasureSpecMode.Exactly)
            {
                throw new Exception("ViewSwithcer can only be used in EXACTLY mode.");
            }

            MeasureSpecMode heightMode = MeasureSpec.GetMode(heightMeasureSpec);
            if (heightMode != MeasureSpecMode.Exactly)
            {
                throw new Exception("ViewSwithcer can only be used in EXACTLY mode.");
            }

            int count = this.ChildCount;
            for (int i = 0; i < count; i++)
                GetChildAt(i).Measure(widthMeasureSpec, heightMeasureSpec);

            if (mFirstLayout)
            {
                ScrollTo(mCurrentScreen * width, 0);
                mFirstLayout = false;
            }
            else if (width != mLastSeenLayoutWidth)
            {
                Display display = ((IWindowManager)Context.GetSystemService(Context.WindowService)).DefaultDisplay;
                int displayWidth = display.Width;

                mNextScreen = Math.Max(0, Math.Min(mCurrentScreen, this.ChildCount - 1));
                int newX = mNextScreen * displayWidth;
                int delta = newX - this.ScrollX;

                mScroller.StartScroll(this.ScrollX, 0, delta, 0);
            }

            mLastSeenLayoutWidth = width;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            int childLeft = 0;
            int count = this.ChildCount;

            for (int i = 0; i < count; i++)
            {
                View child = GetChildAt(i);
                if (child.Visibility != ViewStates.Gone)
                {
                    int childWidth = child.MeasuredWidth;
                    child.Layout(childLeft, 0, childLeft + childWidth, child.MeasuredHeight);
                    childLeft += childWidth;
                }
            }
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            MotionEventActions action = ev.Action;
            bool intercept = false;

            switch (action)
            {
                case MotionEventActions.Move:
                    if (mTouchState == TOUCH_STATE_HORIZONTAL_SCROLLING)
                        intercept = true;
                    else if (mTouchState == TOUCH_STATE_VERTICAL_SCROLLING)
                        intercept = false;
                    else
                    {
                        float x = ev.GetX();
                        int xDiff = (int)Math.Abs(x - mLastMotionX);
                        bool xMoved = xDiff > mTouchSlop;

                        if (xMoved)
                        {
                            mTouchState = TOUCH_STATE_HORIZONTAL_SCROLLING;
                            mLastMotionX = x;
                        }

                        float y = ev.GetY();
                        int yDiff = (int)Math.Abs(y - mLastMotionY);
                        bool yMoved = yDiff > mTouchSlop;

                        if (yMoved)
                            mTouchState = TOUCH_STATE_VERTICAL_SCROLLING;
                    }
                    break;
                case MotionEventActions.Cancel:
                    mTouchState = TOUCH_STATE_REST;
                    break;
                case MotionEventActions.Up:
                    mTouchState = TOUCH_STATE_REST;
                    break;
                case MotionEventActions.Down:
                    mLastMotionX = ev.GetX();
                    mLastMotionY = ev.GetY();
                    break;
                default:
                    break;
            }

            return intercept;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (mVelocityTracker == null)
                mVelocityTracker = VelocityTracker.Obtain();
            mVelocityTracker.AddMovement(e);

            MotionEventActions action = e.Action;
            float x = e.GetX();

            switch (action)
            {
                case MotionEventActions.Down:
                    if (!mScroller.IsFinished)
                        mScroller.AbortAnimation();

                    mLastMotionX = x;

                    if (mScroller.IsFinished)
                        mTouchState = TOUCH_STATE_REST;
                    else
                        mTouchState = TOUCH_STATE_HORIZONTAL_SCROLLING;
                    break;
                case MotionEventActions.Move:
                    int xDiff = (int)Math.Abs(x - mLastMotionX);
                    bool xMoved = xDiff > mTouchSlop;

                    if (xMoved)
                        mTouchState = TOUCH_STATE_HORIZONTAL_SCROLLING;

                    if (mTouchState == TOUCH_STATE_HORIZONTAL_SCROLLING)
                    {
                        int deltaX = (int)(mLastMotionX - x);
                        mLastMotionX = x;
                        int scrollX = this.ScrollX;

                        if (deltaX < 0)
                        {
                            if (scrollX > 0)
                                ScrollBy(Math.Max(-scrollX, deltaX), 0);
                        }
                        else if (deltaX > 0)
                        {
                            if (this.ChildCount >= 1)
                            {
                                int avalableToScroll = this.GetChildAt(this.ChildCount - 1).Right - scrollX - Width;

                                if (avalableToScroll > 0)
                                    ScrollBy(Math.Min(avalableToScroll, deltaX), 0);
                            }
                        }
                    }
                    break;
                case MotionEventActions.Up:
                    if (mTouchState == TOUCH_STATE_HORIZONTAL_SCROLLING)
                    {
                        VelocityTracker velocityTracker = mVelocityTracker;
                        velocityTracker.ComputeCurrentVelocity(VELOCITY_UNIT_PIXELS_PER_SECOND, mMaximumVelocity);
                        int velocityX = (int)velocityTracker.XVelocity;

                        if (velocityX > mDensityAdjustedSnapVelocity && mCurrentScreen > 0)
                            SnapToScreen(mCurrentScreen - 1);
                        else if (velocityX < -mDensityAdjustedSnapVelocity && mCurrentScreen < this.ChildCount - 1)
                            SnapToScreen(mCurrentScreen + 1);
                        else
                            SnapToDestination();

                        if (mVelocityTracker != null)
                        {
                            mVelocityTracker.Recycle();
                            mVelocityTracker = null;
                        }
                        mTouchState = TOUCH_STATE_REST;
                    }
                    break;
                case MotionEventActions.Cancel:
                    mTouchState = TOUCH_STATE_REST;
                    break;
                default:
                    break;
            }
            return true;
        }

        public override void ComputeScroll()
        {
            if (mScroller.ComputeScrollOffset())
            {
                ScrollTo(mScroller.CurrX, mScroller.CurrY);
                PostInvalidate();
            }
            else if (mNextScreen != INVALID_SCREEN)
            {
                mCurrentScreen = Math.Max(0, Math.Min(mNextScreen, this.ChildCount - 1));

                OnChanged(EventArgs.Empty);

                mNextScreen = INVALID_SCREEN;
            }
        }

        public void SetCurrentScreen(int currentScreen, bool animate)
        {
            mCurrentScreen = Math.Max(0, Math.Min(currentScreen, this.ChildCount - 1));

            if (animate)
                SnapToScreen(currentScreen, ANIMATION_SCREEN_DURATION_MILLIS);
            else
                ScrollTo(mCurrentScreen * Width, 0);
            Invalidate();
        }

        private void SnapToDestination()
        {
            int screenWidth = Width;
            int whichScreen = mCurrentScreen;
            int deltaX = ScrollX - (screenWidth * mCurrentScreen);

            if ((deltaX < 0) && mCurrentScreen != 0 && ((screenWidth / FRACTION_OF_SCREEN_WIDTH_FOR_SWIPE < -deltaX)))
                whichScreen--;
            else if ((deltaX > 0) && (mCurrentScreen + 1 != this.ChildCount) &&
                (screenWidth / FRACTION_OF_SCREEN_WIDTH_FOR_SWIPE < deltaX))
                whichScreen++;

            SnapToScreen(whichScreen);
        }

        private void SnapToScreen(int whichScreen)
        {
            SnapToScreen(whichScreen, -1);
        }

        private void SnapToScreen(int whichScreen, int duration)
        {
            mNextScreen = Math.Max(0, Math.Min(whichScreen, this.ChildCount - 1));
            int newX = mNextScreen * Width;
            int delta = newX - ScrollX;

            if (duration < 0)
                mScroller.StartScroll(ScrollX, 0, delta, 0, (int)(Math.Abs(delta) / (float)Width * ANIMATION_SCREEN_DURATION_MILLIS));
            else
                mScroller.StartScroll(ScrollX, 0, delta, 0, duration);

            Invalidate();
        }
    }
}