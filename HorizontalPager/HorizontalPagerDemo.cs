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

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;

namespace Cheesebaron.HorizontalPager
{
    [Activity(Label = "HorizontalPager", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Display display = WindowManager.DefaultDisplay;
            HorizontalPager horiPager = new HorizontalPager(this.ApplicationContext, display);

            int[] backgroundColors = new int[] { Color.Red, Color.Blue, Color.Cyan, Color.Green, Color.Yellow };

            for (int i = 0; i < 5; i++)
            {
                TextView textView = new TextView(this.ApplicationContext);
                textView.Text = (i + 1).ToString();
                textView.TextSize = 100;
                textView.SetTextColor(Color.Black);
                textView.Gravity = GravityFlags.Center;
                textView.SetBackgroundColor(backgroundColors[i]);
                horiPager.AddView(textView);
            }

            SetContentView(horiPager);
        }
    }
}

