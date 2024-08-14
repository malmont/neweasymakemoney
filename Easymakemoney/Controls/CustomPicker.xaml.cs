using System.Collections;
using Microsoft.Maui.Controls;

namespace Easymakemoney.Controls
{
    public partial class CustomPicker : ContentView
    {
        // BindableProperty pour la propriété Title
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(CustomPicker), default(string), BindingMode.TwoWay, propertyChanged: OnTitlePropertyChanged);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CustomPicker)bindable;
            control.pickerControl.Title = (string)newValue;
        }

        // Propriétés ItemsSource et SelectedItem
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(CustomPicker), null, BindingMode.TwoWay, propertyChanged: OnItemsSourceChanged);

        public IList ItemsSource
        {
            get => (IList)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CustomPicker)bindable;
            control.pickerControl.ItemsSource = (IList)newValue;
        }

        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(CustomPicker), null, BindingMode.TwoWay, propertyChanged: OnSelectedItemChanged);

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CustomPicker)bindable;
            control.pickerControl.SelectedItem = newValue;
        }

         public static readonly BindableProperty ItemDisplayBindingProperty =
            BindableProperty.Create(nameof(ItemDisplayBinding), typeof(BindingBase), typeof(CustomPicker), null, BindingMode.OneWay, propertyChanged: OnItemDisplayBindingChanged);

        public BindingBase ItemDisplayBinding
        {
            get => (BindingBase)GetValue(ItemDisplayBindingProperty);
            set => SetValue(ItemDisplayBindingProperty, value);
        }

        private static void OnItemDisplayBindingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CustomPicker)bindable;
            control.pickerControl.ItemDisplayBinding = (BindingBase)newValue;
        }


        public Picker PickerControl => pickerControl;

        public CustomPicker()
        {
            InitializeComponent();
        }
    }
}
