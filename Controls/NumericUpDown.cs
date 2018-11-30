using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using URLServerManagerModern.Controls.Events;

namespace URLServerManagerModern.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:URLServerManagerModern.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:URLServerManagerModern.Controls;assembly=URLServerManagerModern.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:NumericUpDown/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_IncreaseButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_DecreaseButton", Type = typeof(RepeatButton))]
    public class NumericUpDown : Control
    {
        #region Events

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Direct, typeof(ValueChangedHandler), typeof(NumericUpDown));

        public event ValueChangedHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        #endregion

        #region Value Manipulation
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(0m, OnValueChanged, CoerceValue));

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        private decimal previousValue; 

        private static void OnValueChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown control = element as NumericUpDown;
            control.RaiseEvent(new ValueChangedEventArgs(ValueChangedEvent, control.previousValue, control.Value));
        }

        private static object CoerceValue(DependencyObject element, object baseValue)
        {
            decimal value = (decimal)baseValue;
            NumericUpDown control = element as NumericUpDown;

            if (value < control.MinValue)
                value = control.MinValue;
            else if (value > control.MaxValue)
                value = control.MaxValue;

            int decimalPlaces = CountNumberOfDecimalPlaces(value, control.Culture);
            if (control.IsDecimalPointDynamic)
            {
                control.MaxDecimalPlaces = (byte)decimalPlaces;
            }

            if (decimalPlaces > control.MaxDecimalPlaces)
            {
                string stringValue = value.ToString();
                value = decimal.Parse(stringValue.Substring(0, stringValue.Length - (decimalPlaces - control.MaxDecimalPlaces)));
            }

            control.previousValue = control.Value;
            if (control.TextBox != null)
            {
                if (control.IsThousandSeparatorVisible)
                    control.TextBox.Text = value.ToString("N", control.Culture);
                else
                    control.TextBox.Text = value.ToString("F", control.Culture);
            }

            return value;
        }

        private readonly RoutedUICommand increaseValueCommand = new RoutedUICommand("MinorIncreaseValue", "MinorIncreaseValue", typeof(NumericUpDown));
        private readonly RoutedUICommand decreaseValueCommand = new RoutedUICommand("MinorDecreaseValue", "MinorDecreaseValue", typeof(NumericUpDown));

        private void IncreaseValue()
        {
            var value = Utilities.Utilities.ParseStringToDecimal(TextBox.Text);

            if (value + 1 <= MaxValue)
            {
                value++;
            }

            Value = value;
        }

        private void DecreaseValue()
        {
            var value = Utilities.Utilities.ParseStringToDecimal(TextBox.Text);

            if (value - 1 >= MinValue)
            {
                value--;
            }

            Value = value;
        }
        #endregion
        #region Min Max Values

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(decimal), typeof(NumericUpDown), new PropertyMetadata((decimal)int.MinValue, OnMinValueChanged, CoerceMinValue));

        public decimal MinValue
        {
            get { return (decimal)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        private static void OnMinValueChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown control = element as NumericUpDown;
            decimal newValue = (decimal)e.NewValue;
            if (newValue < control.MaxValue)
            {
                control.MinValue = newValue;
            }
        }

        private static object CoerceMinValue(DependencyObject element, object baseValue)
        {
            var minValue = (decimal)baseValue;

            return minValue;
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(decimal), typeof(NumericUpDown), new PropertyMetadata((decimal)int.MaxValue, OnMaxValueChanged, CoerceMaxValue));
        public decimal MaxValue
        {
            get { return (decimal)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        private static void OnMaxValueChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown control = element as NumericUpDown;
            decimal newValue = (decimal)e.NewValue;
            if (newValue > control.MinValue)
            {
                control.MaxValue = newValue;
            }
        }

        private static object CoerceMaxValue(DependencyObject element, object baseValue)
        {
            var maxValue = (decimal)baseValue;

            return maxValue;
        }

        #endregion
        #region Decimal Places

        protected readonly CultureInfo Culture;
        public static readonly DependencyProperty MinDecimalPlacesProperty = DependencyProperty.Register("MinDecimalPlaces", typeof(byte), typeof(NumericUpDown), new PropertyMetadata((byte)0, OnMinDecimalPlacesChanged, CoerceMinDecimalPlaces));

        public byte MinDecimalPlaces
        {
            get { return (byte)GetValue(MinDecimalPlacesProperty); }
            set { SetValue(MinDecimalPlacesProperty, value); }
        }

        private static void OnMinDecimalPlacesChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown control = element as NumericUpDown;
            byte decimalPlaces = (byte)e.NewValue;

            control.Culture.NumberFormat.NumberDecimalDigits = decimalPlaces;

            control.InvalidateProperty(ValueProperty);
        }

        private static object CoerceMinDecimalPlaces(DependencyObject element, object baseValue)
        {
            NumericUpDown control = element as NumericUpDown;
            byte decimalPlaces = (byte)baseValue;

            if (decimalPlaces > 28)
                decimalPlaces = 28;
            if (decimalPlaces > control.MaxDecimalPlaces)
                decimalPlaces = control.MaxDecimalPlaces;

            return decimalPlaces;
        }

        public static readonly DependencyProperty MaxDecimalPlacesProperty = DependencyProperty.Register("MaxDecimalPlaces", typeof(byte), typeof(NumericUpDown), new PropertyMetadata((byte)0, OnMaxDecimalPlacesChanged, CoerceMaxDecimalPlaces));

        public byte MaxDecimalPlaces
        {
            get { return (byte)GetValue(MaxDecimalPlacesProperty); }
            set { SetValue(MaxDecimalPlacesProperty, value); }
        }

        private static void OnMaxDecimalPlacesChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown control = element as NumericUpDown;

            if (control.IsDecimalPointDynamic)
            {
                control.IsDecimalPointDynamic = false;
                control.InvalidateProperty(ValueProperty);
                control.IsDecimalPointDynamic = true;
            }
            else
                control.InvalidateProperty(ValueProperty);
        }

        private static object CoerceMaxDecimalPlaces(DependencyObject element, object baseValue)
        {
            NumericUpDown control = element as NumericUpDown;
            byte decimalPlaces = (byte)baseValue;

            if (decimalPlaces > 28)
                decimalPlaces = 28;

            if (decimalPlaces < control.MinDecimalPlaces)
                decimalPlaces = control.MinDecimalPlaces;

            return decimalPlaces;
        }

        public static readonly DependencyProperty IsDecimalPointDynamicProperty = DependencyProperty.Register("IsDecimalPointDynamic", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

        public bool IsDecimalPointDynamic
        {
            get { return (bool)GetValue(IsDecimalPointDynamicProperty); }
            set { SetValue(IsDecimalPointDynamicProperty, value); }
        }

        public static readonly DependencyProperty IsThousandSeparatorVisibleProperty = DependencyProperty.Register("IsThousandSeparatorVisible", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

        public bool IsThousandSeparatorVisible
        {
            get { return (bool)GetValue(IsThousandSeparatorVisibleProperty); }
            set { SetValue(IsThousandSeparatorVisibleProperty, value); }
        }

        #endregion

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        public NumericUpDown()
        {
            Culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            
            Culture.NumberFormat.NumberDecimalDigits = MinDecimalPlaces;
            Loaded += (sender, args) => InvalidateProperty(ValueProperty);
        }

        #region Attaching

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            AttachToVisualTree();
            AttachCommands();
        }

        private void AttachToVisualTree()
        {
            AttachTextBox();
            AttachIncreaseButton();
            AttachDecreaseButton();
        }


        protected TextBox TextBox;
        private void AttachTextBox()
        {
            TextBox textBox = GetTemplateChild("PART_TextBox") as TextBox;

            if (textBox != null)
            {
                TextBox = textBox;
                TextBox.Text = Value.ToString();
                textBox.PreviewTextInput += OnTextBoxTextChanged;
                DataObject.AddPastingHandler(textBox, DoNotAllowCharPasting);
            }
        }

        protected RepeatButton IncreaseButton;
        private void AttachIncreaseButton()
        {
            RepeatButton button = GetTemplateChild("PART_IncreaseButton") as RepeatButton;
            if (button != null)
            {
                IncreaseButton = button;
                button.Focusable = false;
                button.Command = increaseValueCommand;
                button.PreviewMouseLeftButtonDown += (sender, args) => RemoveFocus();
            }
        }

        protected RepeatButton DecreaseButton;
        private void AttachDecreaseButton()
        {
            RepeatButton button = GetTemplateChild("PART_DecreaseButton") as RepeatButton;
            if (button != null)
            {
                DecreaseButton = button;
                button.Focusable = false;
                button.Command = decreaseValueCommand;
                button.PreviewMouseLeftButtonDown += (sender, args) => RemoveFocus();
            }
        }

        private void AttachCommands()
        {
            CommandBindings.Add(new CommandBinding(increaseValueCommand, (a, b) => IncreaseValue()));
            CommandBindings.Add(new CommandBinding(decreaseValueCommand, (a, b) => DecreaseValue()));
        }

        #endregion

        private void OnTextBoxTextChanged(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox)
            {
                decimal d;
                if (!decimal.TryParse((sender as TextBox).Text + e.Text, NumberStyles.Float, Culture.NumberFormat, out d))
                {
                    e.Handled = true;
                }
                else
                {
                    int start = TextBox.SelectionStart;
                    Value = Utilities.Utilities.ParseStringToDecimal((sender as TextBox).Text + e.Text);
                    e.Handled = true;
                    if(start + 1 <= TextBox.Text.Length)
                    TextBox.SelectionStart = start + 1;
                }
            }
        }

        private void DoNotAllowCharPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                decimal d;
                if (!decimal.TryParse((string)e.DataObject.GetData(typeof(string)), out d))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void RemoveFocus()
        {
            Focusable = true;
            Focus();
            Focusable = false;
        }

        private static int CountNumberOfDecimalPlaces(object number)
        {
            if (number == null || !(number is double || number is float || number is decimal || number is string))
                return 0;
            string[] decimalPointAsString = number.ToString().Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (decimalPointAsString.Length > 1)
                return decimalPointAsString[1].Length;
            return 0;
        }

        private static int CountNumberOfDecimalPlaces(object number, IFormatProvider provider)
        {
            if (provider == null || !(provider is CultureInfo || provider is NumberFormatInfo))
                return CountNumberOfDecimalPlaces(number);
            if (number == null || !(number is double || number is float || number is decimal || number is string))
                return 0;

            if (provider is CultureInfo)
                provider = (provider as CultureInfo).NumberFormat;
            return number.ToString().SkipWhile(x => x.ToString() != (provider as NumberFormatInfo).NumberDecimalSeparator).Skip(1).Count();
        }
    }
}
