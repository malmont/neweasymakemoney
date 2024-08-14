using Android.Graphics.Drawables;
using Android.Widget;
using Easymakemoney.Controls;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;

namespace Easymakemoney.Handlers
{
    public class CustomPickerHandler : PickerHandler
    {
        public CustomPickerHandler() : base()
        {
            // Customize the mapping of the control here if needed
            Mapper.AppendToMapping(nameof(CustomPicker), (handler, view) =>
            {
#if ANDROID
                if (handler.PlatformView is EditText nativeView)
                {
                    // Remove the default underline on Android
                    nativeView.Background = null;

                    // Create a custom background with rounded corners
                    var gradientDrawable = new GradientDrawable();
                    gradientDrawable.SetCornerRadius(20f);  // Adjust the corner radius
                    gradientDrawable.SetStroke(2, Android.Graphics.Color.Gray);  // Border color and width
                    gradientDrawable.SetColor(Android.Graphics.Color.White);  // Background color
                    nativeView.SetBackground(gradientDrawable);
                }
#endif
            });
        }
    }
}
