#if ANDROID
using Android.Graphics;
#elif IOS
using UIKit;
#endif

namespace Easymakemoney
{
    public partial class App : Application
    {
        public static UserBasicInfo? UserDetails;
        public static string? Token;

        public App()
        {
            InitializeComponent();

            // Mapping personnalisé pour BorderlessEntry
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry), (handler, view) =>
            {
                if (view is BorderlessEntry)
                {
#if ANDROID
                    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
                }
            });

            // Mapping personnalisé pour BorderlessDatePicker
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(nameof(BorderlessDatePicker), (handler, view) =>
            {
                if (view is BorderlessDatePicker)
                {
#if ANDROID
                    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
                }
            });

            MainPage = new AppShell();
        }
    }
}
