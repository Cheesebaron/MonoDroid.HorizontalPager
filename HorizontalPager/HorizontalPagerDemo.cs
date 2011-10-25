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

