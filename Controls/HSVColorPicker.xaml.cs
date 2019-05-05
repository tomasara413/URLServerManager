using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    //TODO: Get position from color
    public partial class HSVColorPicker : UserControl
    {
        private MemoryStream ms;
        private Bitmap bmp;
        private BitmapImage img;
        private System.Drawing.Color hue;

        public HSVColorPicker() : this(255, 0, 0) { }

        public HSVColorPicker(int red, int green, int blue)
        {
            InitializeComponent();
            bmp = new Bitmap(255, 255);

            //PickedDetector.Visibility = Visibility.Visible;
            //PickedDetector.Margin = new Thickness(0, PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);

            ChangeHue(System.Drawing.Color.FromArgb(red, green, blue));
        }

        string activePicker = "H";

        private void ChangeHue(System.Drawing.Color hue)
        {
            //Debug.WriteLine(hue.ToString());
            if (bmp == null)
                return;
            double saturation, value, saturatedR, saturatedG, saturatedB;
            double saturationToThird;
            double hueValue = GetHueLogicValue();
            switch (activePicker)
            {
                case "H":
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            saturation = (double)y / (bmp.Height - 1);
                            value = (double)x / (bmp.Width - 1);
                            saturatedR = hue.R + saturation * (255 - hue.R);
                            saturatedG = hue.G + saturation * (255 - hue.G);
                            saturatedB = hue.B + saturation * (255 - hue.B);

                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)(saturatedR - saturatedR * (1 - value)), (byte)(saturatedG - saturatedG * (1 - value)), (byte)(saturatedB - saturatedB * (1 - value))));
                        }
                    }
                    break;
                case "S":
                    double highliestSaturated;
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            saturation = (double)y / (bmp.Height - 1);
                            value = (double)x / (bmp.Width - 1);
                            if (saturation <= oneThird)
                            {
                                saturationToThird = saturation / oneThird;
                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedR = 255 * value;
                                    saturatedB = 255 * saturationToThird * value;
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedR = 255 * (1 - saturationToThird) * value;
                                    saturatedB = 255 * value;
                                }

                                saturatedG = 0;
                            }
                            else if (saturation > oneThird && saturation <= 2 * oneThird)
                            {
                                saturationToThird = (saturation - oneThird) / oneThird;

                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedG = 255 * saturationToThird * value;
                                    saturatedB = 255 * value;

                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedG = 255 * value;
                                    saturatedB = 255 * (1 - saturationToThird) * value;
                                }

                                saturatedR = 0;
                            }
                            else
                            {
                                saturationToThird = (saturation - 2 * oneThird) / oneThird;
                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedR = 255 * saturationToThird * value;
                                    saturatedG = 255 * value;
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedR = 255 * value;
                                    saturatedG = 255 * (1 - saturationToThird) * value;
                                }

                                saturatedB = 0;
                            }

                            highliestSaturated = saturatedR > saturatedB ? (saturatedR > saturatedG ? saturatedR : saturatedG) : (saturatedB > saturatedG ? saturatedB : saturatedG);

                            saturatedR += (highliestSaturated - saturatedR) * hueValue;
                            saturatedG += (highliestSaturated - saturatedG) * hueValue;
                            saturatedB += (highliestSaturated - saturatedB) * hueValue;



                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)saturatedR, (byte)saturatedG, (byte)saturatedB));
                            //Debug.Write(bmp.GetPixel(x, y));
                        }
                    }
                    break;
                case "V":
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            saturation = (double)y / (bmp.Height - 1);
                            value = (double)x / (bmp.Width - 1);
                            if (saturation <= oneThird)
                            {
                                saturationToThird = saturation / oneThird;
                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedR = 255 * (1 - hueValue);
                                    saturatedB = 255 * saturationToThird * (1 - hueValue);
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedR = 255 * (1 - saturationToThird) * (1 - hueValue);
                                    saturatedB = 255 * (1 - hueValue);
                                }

                                saturatedG = 0;
                            }
                            else if (saturation > oneThird && saturation <= 2 * oneThird)
                            {
                                saturationToThird = (saturation - oneThird) / oneThird;

                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedG = 255 * saturationToThird * (1 - hueValue);
                                    saturatedB = 255 * (1 - hueValue);
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedG = 255 * (1 - hueValue);
                                    saturatedB = 255 * (1 - saturationToThird) * (1 - hueValue);
                                }

                                saturatedR = 0;
                            }
                            else
                            {
                                saturationToThird = (saturation - 2 * oneThird) / oneThird;
                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedR = 255 * saturationToThird * (1 - hueValue);
                                    saturatedG = 255 * (1 - hueValue);
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedR = 255 * (1 - hueValue);
                                    saturatedG = 255 * (1 - saturationToThird) * (1 - hueValue);
                                }

                                saturatedB = 0;
                            }

                            saturatedR += (255 - saturatedR) * (1 - value) * (1 - hueValue);
                            saturatedG += (255 - saturatedG) * (1 - value) * (1 - hueValue);
                            saturatedB += (255 - saturatedB) * (1 - value) * (1 - hueValue);

                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)saturatedR, (byte)saturatedG, (byte)saturatedB));
                        }
                    }
                    break;
                case "R":
                    //Debug.WriteLine(hue);
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            saturation = (double)y / (bmp.Height - 1);
                            value = (double)x / (bmp.Width - 1);

                            saturatedR = hue.R;
                            saturatedG = 255 * (1 - saturation);//hue.G + saturation * (255 - hue.G);
                            saturatedB = 255 * value;//hue.B + saturation * (255 - hue.B);

                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)saturatedR, (byte)saturatedG, (byte)saturatedB));
                        }
                    }
                    break;
                case "G":
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            saturation = (double)y / (bmp.Height - 1);
                            value = (double)x / (bmp.Width - 1);
                            saturatedR = 255 * (1 - saturation);
                            saturatedG = hue.G;
                            saturatedB = 255 * value;

                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)saturatedR, (byte)saturatedG, (byte)saturatedB));
                        }
                    }
                    break;
                case "B":
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            saturation = (double)y / (bmp.Height - 1);
                            value = (double)x / (bmp.Width - 1);
                            saturatedR = 255 * (1 - saturation);
                            saturatedG = 255 * value;
                            saturatedB = hue.B;

                            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)saturatedR, (byte)saturatedG, (byte)saturatedB));
                        }
                    }
                    break;
            }

            this.hue = hue;

            ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            img = new BitmapImage();

            using (ms)
            {
                img.BeginInit();
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();

                ImageBox.ImageSource = img;
            }
        }

        const double oneThird = 1d / 3d;
        private void OnPalleteInteraction(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                if ((sender as Button).Name == "PickHue")
                {
                    //Debug.WriteLine(Mouse.GetPosition(PickHue));
                    double positionY = Mouse.GetPosition(PickHue).Y;
                    positionY = Clamp(positionY, 0, PickHue.ActualHeight);
                    HueDetector.Margin = new Thickness(0, positionY - HueDetector.ActualHeight / 2, 0, 0);

                    double relativeY = GetHueLogicValue();
                    //Debug.WriteLine("Relative y: " + relativeY);

                    switch (activePicker)
                    {
                        case "H":
                            double saturationToThird, saturatedR, saturatedG, saturatedB;
                            if (relativeY <= oneThird)
                            {
                                saturationToThird = relativeY / oneThird;
                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedR = 255;
                                    saturatedB = 255 * saturationToThird;
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedR = 255 * (1 - saturationToThird);
                                    saturatedB = 255;
                                }

                                saturatedG = 0;
                            }
                            else if (relativeY > oneThird && relativeY <= 2 * oneThird)
                            {
                                saturationToThird = (relativeY - oneThird) / oneThird;

                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedG = 255 * saturationToThird;
                                    saturatedB = 255;

                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedG = 255;
                                    saturatedB = 255 * (1 - saturationToThird);
                                }

                                saturatedR = 0;
                            }
                            else
                            {
                                saturationToThird = (relativeY - 2 * oneThird) / oneThird;
                                if (saturationToThird < 0.5)
                                {
                                    saturationToThird *= 2;
                                    saturatedR = 255 * saturationToThird;
                                    saturatedG = 255;
                                }
                                else
                                {
                                    saturationToThird = (saturationToThird - 0.5) * 2;
                                    saturatedR = 255;
                                    saturatedG = 255 * (1 - saturationToThird);
                                }

                                saturatedB = 0;
                            }

                            ChangeHue(System.Drawing.Color.FromArgb((byte)saturatedR, (byte)saturatedG, (byte)saturatedB));
                            break;
                        case "S":
                        case "V":
                            ChangeHue(hue);
                            break;
                        case "R":
                            //Debug.WriteLine(1 - relativeY);
                            ChangeHue(System.Drawing.Color.FromArgb((byte)Math.Round((1 - relativeY) * 255), hue.G, hue.B));
                            R.Value = hue.R;
                            break;
                        case "G":
                            ChangeHue(System.Drawing.Color.FromArgb(hue.A, (byte)Math.Round((1 - relativeY) * 255), hue.B));
                            G.Value = hue.G;
                            break;
                        case "B":
                            ChangeHue(System.Drawing.Color.FromArgb(hue.A, hue.G, (byte)Math.Round((1 - relativeY) * 255)));
                            B.Value = hue.B;
                            break;
                    }


                }
                else
                {
                    //Debug.WriteLine(Mouse.GetPosition(PickColor));
                    System.Windows.Point position = Mouse.GetPosition(PickColor);
                    /*if (PickedDetector.Visibility != Visibility.Visible)
                    {
                        PickedDetector.Visibility = Visibility.Visible;
                        PickedDetector.UpdateLayout();
                    }*/
                    position.X = Clamp(position.X, 0, PickColor.ActualWidth - 1);
                    position.Y = Clamp(position.Y, 0, PickColor.ActualHeight - 1);
                    PickedDetector.Margin = new Thickness(position.X - PickedDetector.ActualWidth / 2, position.Y - PickedDetector.ActualHeight / 2, 0, 0);

                }
            }
            else if (sender is RadioButton)
            {
                if ((sender as RadioButton).Content != null)
                {
                    activePicker = (sender as RadioButton).Content.ToString();

                    switch (activePicker)
                    {
                        case "H":
                            Hues.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(255, 0, 0), 0), new GradientStop(System.Windows.Media.Color.FromRgb(255, 0, 255), 0.16), new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 255), 0.32), new GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 255), 0.48), new GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 0), 0.64), new GradientStop(System.Windows.Media.Color.FromRgb(255, 255, 0), 0.8), new GradientStop(System.Windows.Media.Color.FromRgb(255, 0, 0), 1) }));

                            //double c = ;
                            hue = GetRGBFromHSV(H.Value, 1, 1);
                            break;
                        case "S":
                        case "V":
                            Hues.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(255, 255, 255), 0), new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 0), 1) }));
                            break;
                        case "R":
                            Hues.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(255, 0, 0), 0), new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 0), 1) }));
                            hue = GetPickedColor();
                            break;
                        case "G":
                            Hues.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 0), 0), new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 0), 1) }));
                            hue = GetPickedColor();
                            break;
                        case "B":
                            Hues.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 255), 0), new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 0), 1) }));
                            hue = GetPickedColor();
                            break;
                    }
                }
                ChangeHue(hue);
            }

            //Debug.WriteLine("Selected color: " + GetPickedColor());

            if (R != null)
                UpdateFields();
        }

        private System.Drawing.Color GetPickedColor()
        {
            System.Drawing.Color pickedColor;
            if (PickedDetector.Visibility != Visibility.Visible)
                return pickedColor = System.Drawing.Color.Black;
            else
            {
                Point sv = GetPickedSaturationAndValue();

                return pickedColor = bmp.GetPixel((int)Math.Round(sv.y * (bmp.Width - 1)), (int)Math.Round(sv.x * (bmp.Height - 1)));
            }
        }

        private double GetHueLogicValue()
        {
            //Debug.WriteLine("Value: " + (HueDetector.Margin.Top + Math.Round(HueDetector.ActualHeight / 2)) / (PickHue.ActualHeight - HueDetector.ActualHeight));
            return Clamp(Math.Round((HueDetector.Margin.Top + Math.Round(HueDetector.ActualHeight / 2)) / (PickHue.ActualHeight - HueDetector.ActualHeight), 3), 0, 1);
        }

        private Point GetPickedSaturationAndValue()
        {
            //Debug.WriteLine("Top: " + Math.Round((PickedDetector.Margin.Top + PickedOffset.ActualHeight) / (PickColor.ActualHeight - PickedDetectorSquare.ActualHeight), 3));
            return new Point(Clamp(Math.Round((PickedDetector.Margin.Top + PickedOffset.ActualHeight) / (PickColor.ActualHeight - PickedDetectorSquare.ActualHeight), 3), 0, 1), Clamp(Math.Round((PickedDetector.Margin.Left + PickedOffset.ActualWidth) / (PickColor.ActualWidth - PickedDetectorSquare.ActualWidth), 3), 0, 1));
        }

        private bool ChangeFromWithin = false;
        private void UpdateFields()
        {
            System.Drawing.Color pickedColor = GetPickedColor();

            ChangeFromWithin = true;
            R.Value = pickedColor.R;
            G.Value = pickedColor.G;
            B.Value = pickedColor.B;

            decimal[] hsv = GetHSVFromRGB(pickedColor);
            H.Value = hsv[0];
            S.Value = hsv[1] * 100;
            V.Value = hsv[2] * 100;
            ChangeFromWithin = false;
            HTML.Text = ((byte)R.Value).ToString("X2") + ((byte)G.Value).ToString("X2") + ((byte)B.Value).ToString("X2");

            UpdateColorBackgrounds(hsv[1], hsv[2], pickedColor);
        }

        private void UpdateColorBackgrounds(decimal saturation, decimal value, System.Drawing.Color color)
        {
            UpdateColorBackgrounds(saturation, value, color.R, color.G, color.B);
        }

        private void UpdateColorBackgrounds(decimal saturation, decimal value, byte R, byte G, byte B)
        {
            saturation = 1 - saturation;

            decimal[] hsv = GetHSVFromRGB(R, G, B);
            System.Drawing.Color original = GetRGBFromHSV(hsv[0], 1, 1);
            HBorder.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value), (byte)(255 * value * saturation), (byte)(255 * value * saturation)), 0), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value), (byte)(255 * value), (byte)(255 * value * saturation)), 0.16), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value * saturation), (byte)(255 * value), (byte)(255 * value * saturation)), 0.32), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value * saturation), (byte)(255 * value), (byte)(255 * value)), 0.48), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value * saturation), (byte)(255 * value * saturation), (byte)(255 * value)), 0.64), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value), (byte)(255 * value * saturation), (byte)(255 * value)), 0.8), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(255 * value), (byte)(255 * value * saturation), (byte)(255 * value * saturation)), 1) }), 0);
            SBorder.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb((byte)(value * 255), (byte)(value * 255), (byte)(value * 255)), 0), new GradientStop(System.Windows.Media.Color.FromRgb((byte)(original.R * value), (byte)(original.G * value), (byte)(original.B * value)), 1) }));
            original = GetRGBFromHSV(hsv[0], 1 - saturation, 1);
            VBorder.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(0, 0, 0), 0), new GradientStop(System.Windows.Media.Color.FromRgb(original.R, original.G, original.B), 1) }));
            RBorder.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(0, G, B), 0), new GradientStop(System.Windows.Media.Color.FromRgb(255, G, B), 1) }));
            GBorder.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(R, 0, B), 0), new GradientStop(System.Windows.Media.Color.FromRgb(R, 255, B), 1) }));
            BBorder.Background = new LinearGradientBrush(new GradientStopCollection(new GradientStop[] { new GradientStop(System.Windows.Media.Color.FromRgb(R, G, 0), 0), new GradientStop(System.Windows.Media.Color.FromRgb(R, G, 255), 1) }));
            CurrentColor.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(R, G, B));
        }

        private bool ModifyOtherFieldValues = true;
        private void OnFieldChanged(object sender, ValueChangedEventArgs e)
        {
            if (ChangeFromWithin)
                return;

            NumericUpDown n = (sender as NumericUpDown);
            if (n != null)
            {
                ChangeFromWithin = true;
                decimal newValue = (decimal)e.NewValue;

                decimal BackupR = R.Value;
                decimal BackupG = G.Value;
                decimal BackupB = B.Value;
                decimal BackupH = H.Value;
                decimal BackupS = S.Value;
                decimal BackupV = V.Value;

                //Debug.WriteLine("Field Changed: " + (sender as NumericUpDown).Name);
                switch (n.Name)
                {
                    case "H":
                        switch (activePicker)
                        {
                            case "H":
                                HueDetector.Margin = new Thickness(0, ((double)(360 - newValue) / 360) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);

                                newValue /= 60;
                                byte r = 0, g = 0, b = 0;
                                double correctedValue = (double)(newValue - (int)newValue);
                                if (newValue <= 1 || newValue >= 5)
                                    r = 255;
                                if (newValue >= 1 && newValue <= 3)
                                    g = 255;
                                if (newValue >= 3 && newValue <= 5)
                                    b = 255;

                                switch ((int)newValue)
                                {
                                    case 0:
                                        //Debug.WriteLine(correctedValue + " " + newValue);
                                        g = (byte)(correctedValue * 255);
                                        break;
                                    case 1:
                                        r = (byte)((1 - correctedValue) * 255);
                                        break;
                                    case 2:
                                        b = (byte)(correctedValue * 255);
                                        break;
                                    case 3:
                                        g = (byte)((1 - correctedValue) * 255);
                                        break;
                                    case 4:
                                        r = (byte)(correctedValue * 255);
                                        break;
                                    case 5:
                                        b = (byte)((1 - correctedValue) * 255);
                                        break;
                                }


                                ChangeHue(System.Drawing.Color.FromArgb(r, g, b));
                                break;
                            case "S":
                            case "V":
                                PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, ((double)(360 - newValue) / 360) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                break;
                            case "R":
                                double valueMod = (double)(V.Value / 100);
                                double mode = (double)newValue / 60;
                                double modMode = mode;
                                if (mode <= 1)
                                {
                                    modMode = mode;
                                    modMode *= valueMod;
                                    G.Value = (decimal)modMode * 255;
                                    PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, (1 - modMode) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                }

                                if (mode >= 1 && mode <= 2)
                                {
                                    modMode = mode;
                                    modMode -= 1;
                                    modMode *= valueMod;

                                    R.Value = (decimal)(1 * valueMod - modMode) * 255;
                                    HueDetector.Margin = new Thickness(0, (1 - (double)(R.Value / 255)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                    ChangeHue(System.Drawing.Color.FromArgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
                                }

                                if (mode >= 2 && mode <= 3)
                                {
                                    modMode = mode;
                                    modMode -= 2;
                                    modMode *= valueMod;

                                    B.Value = (decimal)modMode * 255;
                                    PickedDetector.Margin = new Thickness((double)(B.Value / 255) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, PickedDetector.Margin.Top, 0, 0);
                                }

                                if (mode >= 3 && mode <= 4)
                                {
                                    modMode = mode;
                                    modMode -= 3;
                                    G.Value = (decimal)(1 - modMode) * 255;
                                    G.Value *= (decimal)valueMod;
                                    PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, (1 - (double)(G.Value / 255)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                }

                                if (mode >= 4 && mode <= 5)
                                {
                                    modMode = mode;
                                    modMode -= 4;
                                    modMode *= valueMod;
                                    R.Value = (decimal)modMode * 255;
                                    HueDetector.Margin = new Thickness(0, (1 - (double)(R.Value / 255)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                    ChangeHue(System.Drawing.Color.FromArgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
                                }

                                if (mode >= 5 && mode <= 6)
                                {
                                    mode -= 5;
                                    B.Value = (decimal)(1 - mode) * 255;
                                    B.Value *= (decimal) valueMod;

                                    PickedDetector.Margin = new Thickness((double)(B.Value / 255) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, PickedDetector.Margin.Top, 0, 0);
                                }
                                break;
                            case "G":
                                valueMod = (double)(V.Value / 100);
                                mode = (double)newValue / 60;
                                if (mode <= 1)
                                {
                                    modMode = mode;
                                    modMode *= valueMod;

                                    G.Value = (decimal)modMode * 255;
                                    HueDetector.Margin = new Thickness(0, (1 - (double)(G.Value / 255)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                    ChangeHue(System.Drawing.Color.FromArgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
                                }

                                if (mode >= 1 && mode <= 2)
                                {
                                    modMode = mode;
                                    modMode -= 1;
                                    modMode *= valueMod;

                                    
                                    R.Value = (decimal)(1 - modMode) * 255;
                                    PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, ((1 - valueMod) + (1 - (double)(R.Value / 255))) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                }

                                if (mode >= 2 && mode <= 3)
                                {
                                    modMode = mode;
                                    modMode -= 2;
                                    modMode *= valueMod;

                                    B.Value = (decimal)modMode * 255;
                                    PickedDetector.Margin = new Thickness((double)(B.Value / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                }

                                if (mode >= 3 && mode <= 4)
                                {
                                    modMode = mode;
                                    modMode -= 3;
                                    modMode *= valueMod;
                                    G.Value = (decimal)(1 * valueMod - modMode) * 255;
                                    HueDetector.Margin = new Thickness(0, (1 - (double)(G.Value / 255)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                    ChangeHue(System.Drawing.Color.FromArgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
                                }

                                if (mode >= 4 && mode <= 5)
                                {
                                    modMode = mode;
                                    modMode -= 4;
                                    modMode *= valueMod;
                                    R.Value = (decimal)modMode * 255;
                                    R.Value *= (decimal)valueMod;
                                    PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, (1 - (double)(R.Value / 255)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                }

                                if (mode >= 5 && mode <= 6)
                                {
                                    mode -= 5;
                                    B.Value = (decimal)(1 - mode) * 255;
                                    B.Value *= (decimal)valueMod;

                                    PickedDetector.Margin = new Thickness((double)(B.Value / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                }
                                break;
                            case "B":
                                valueMod = (double)(V.Value / 100);
                                mode = (double)newValue / 60;
                                if (mode <= 1)
                                {
                                    modMode = mode;
                                    modMode *= valueMod;

                                    G.Value = (decimal)modMode * 255;
                                    PickedDetector.Margin = new Thickness((double)(G.Value / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                }

                                if (mode >= 1 && mode <= 2)
                                {
                                    modMode = mode;
                                    modMode -= 1;
                                    modMode *= valueMod;


                                    R.Value = (decimal)(1 - modMode) * 255;
                                    PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, ((1 - valueMod) + (1 - (double)(R.Value / 255))) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                }

                                if (mode >= 2 && mode <= 3)
                                {
                                    modMode = mode;
                                    modMode -= 2;
                                    modMode *= valueMod;

                                    B.Value = (decimal)modMode * 255;
                                    HueDetector.Margin = new Thickness(0, (1 - (double)(B.Value / 255)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                    ChangeHue(System.Drawing.Color.FromArgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
                                }

                                if (mode >= 3 && mode <= 4)
                                {
                                    modMode = mode;
                                    modMode -= 3;
                                    modMode *= valueMod;
                                    G.Value = (decimal)(1 * valueMod - modMode) * 255;
                                    PickedDetector.Margin = new Thickness((double)(G.Value / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                }

                                if (mode >= 4 && mode <= 5)
                                {
                                    modMode = mode;
                                    modMode -= 4;
                                    modMode *= valueMod;
                                    R.Value = (decimal)modMode * 255;
                                    R.Value *= (decimal)valueMod;
                                    PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, (1 - (double)(R.Value / 255)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                }

                                if (mode >= 5 && mode <= 6)
                                {
                                    mode -= 5;
                                    B.Value = (decimal)(1 - mode) * 255;
                                    B.Value *= (decimal)valueMod;

                                    HueDetector.Margin = new Thickness(0, (double)(B.Value / 255) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                    ChangeHue(System.Drawing.Color.FromArgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
                                }
                                break;
                        }

                        //Debug.WriteLine("R: " + r + ", G:" + g + ", B: " + b);
                        
                        break;
                    case "S":
                        switch (activePicker)
                        {
                            case "H":
                                PickedDetector.Margin = new Thickness(PickedDetector.Margin.Left, (1 - (double)(newValue / 100)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                System.Drawing.Color c = GetRGBFromHSV(H.Value, newValue / 100, V.Value / 100);

                                R.Value = c.R;
                                G.Value = c.G;
                                B.Value = c.B;
                                break;
                            case "S":
                                HueDetector.Margin = new Thickness(0, (1 - (double)(newValue / 100)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                ChangeHue(GetRGBFromHSV(H.Value, newValue / 100, V.Value / 100));
                                break;
                            case "V":
                                PickedDetector.Margin = new Thickness((double)(newValue / 100) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                break;
                            case "R":
                                c = GetRGBFromHSV(H.Value, newValue / 100, V.Value / 100);
                                R.Value = c.R;
                                G.Value = c.G;
                                B.Value = c.B;

                                PickedDetector.Margin = new Thickness(((double)c.B / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, (1 - ((double)c.G / 255)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                break;
                            case "G":
                                c = GetRGBFromHSV(H.Value, newValue / 100, V.Value / 100);
                                R.Value = c.R;
                                G.Value = c.G;
                                B.Value = c.B;

                                PickedDetector.Margin = new Thickness(((double)c.B / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, (1 - ((double)c.R / 255)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                break;
                            case "B":
                                c = GetRGBFromHSV(H.Value, newValue / 100, V.Value / 100);
                                R.Value = c.R;
                                G.Value = c.G;
                                B.Value = c.B;

                                PickedDetector.Margin = new Thickness(((double)c.G / 255) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, (1 - ((double)c.R / 255)) * PickColor.ActualHeight - PickedDetector.ActualHeight / 2, 0, 0);
                                break;
                        }
                        break;
                    case "V":
                        switch (activePicker)
                        {
                            case "H":
                                PickedDetector.Margin = new Thickness((double)(newValue / 100) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                System.Drawing.Color c = GetRGBFromHSV(H.Value, S.Value / 100, newValue / 100);
                                R.Value = c.R;
                                G.Value = c.G;
                                B.Value = c.B;
                                break;
                            case "S":
                                PickedDetector.Margin = new Thickness((double)(newValue / 100) * PickColor.ActualWidth - PickedDetector.ActualWidth / 2, PickedDetector.Margin.Top, 0, 0);
                                break;
                            case "V":
                                HueDetector.Margin = new Thickness(0, (1 - (double)(newValue / 100)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                ChangeHue(GetRGBFromHSV(H.Value, S.Value / 100, newValue / 100));
                                break;
                            case "R":
                            case "G":
                            case "B":
                                HueDetector.Margin = new Thickness(0, (1 - (double)(newValue / 100)) * HuePicker.ActualHeight - HueDetector.ActualHeight / 2, 0, 0);
                                ChangeHue(GetRGBFromHSV(H.Value, S.Value / 100, newValue / 100));
                                ChangeFromWithin = false;
                                OnFieldChanged(S, new ValueChangedEventArgs(NumericUpDown.ValueChangedEvent, S.Value, S.Value));

                                break;
                        }
                        break;
                    case "R":
                    case "G":
                    case "B":
                        decimal[] hsv = GetHSVFromRGB((byte)R.Value, (byte)G.Value, (byte)B.Value);
                        ChangeFromWithin = false;
                        ModifyOtherFieldValues = false;
                        H.Value = hsv[0];
                        ModifyOtherFieldValues = false;
                        S.Value = hsv[1] * 100;
                        ModifyOtherFieldValues = false;
                        V.Value = hsv[2] * 100;
                        ModifyOtherFieldValues = true;
                        break;
                }
                ChangeFromWithin = true;

                if (!ModifyOtherFieldValues)
                {
                    if(n.Name != "R")
                        R.Value = BackupR;
                    if (n.Name != "G")
                        G.Value = BackupG;
                    if (n.Name != "B")
                        B.Value = BackupB;
                    if (n.Name != "H")
                        H.Value = BackupH;
                    if (n.Name != "S")
                        S.Value = BackupS;
                    if (n.Name != "V")
                        V.Value = BackupV;
                }

                UpdateColorBackgrounds(S.Value / 100, V.Value / 100, GetPickedColor());
                ChangeFromWithin = false;
                ModifyOtherFieldValues = true;

                HTML.Text = ((byte)R.Value).ToString("X2") + ((byte)G.Value).ToString("X2") + ((byte)B.Value).ToString("X2");
            }
        }

        private void OnPickerLoaded(object sender, RoutedEventArgs e)
        {
            PickedDetector.Visibility = Visibility.Visible;
            PickedDetector.Height = PickColor.ActualHeight * 2 + 3;
            //Debug.WriteLine(PickedDetector.ActualWidth);
            PickedDetector.UpdateLayout();
            //Debug.WriteLine(PickedDetector.ActualWidth);

            PickedDetector.Margin = new Thickness(- PickedDetector.ActualWidth / 2, -PickedDetector.ActualHeight / 2 + PickColor.ActualHeight, 0, 0);
        }

        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        private void OnPalleteInteractionMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                OnPalleteInteraction(sender, null);
        }

        /**
         * <summary>
         * Returns a color based on hue.
         * </summary>
         * <param name="hue">Hue (0 - 360)</param>
         * <param name="saturation">Saturation (0 - 1)</param>
         * <param name="value">Value (0 - 1)</param>
         **/
        private static System.Drawing.Color GetRGBFromHSV(decimal hue, decimal saturation, decimal value)
        {
            decimal chroma = value * saturation;
            decimal hmed = hue / 60;
            decimal x = chroma * (1 - Math.Abs(hmed % 2 - 1));
            decimal m = value - chroma;
            //Debug.WriteLine("{0}, {1}, {2}",(byte)(chroma + m) * 255, (byte)(x + m) * 255, (byte) (m * 255));

            if (hmed >= 0 && hmed < 1)
                return System.Drawing.Color.FromArgb((byte)((chroma + m) * 255), (byte)((x + m) * 255), (byte)(m * 255));
            if (hmed >= 1 && hmed < 2)
                return System.Drawing.Color.FromArgb((byte)((x + m) * 255), (byte)((chroma + m) * 255), (byte)(m * 255));
            if (hmed >= 2 && hmed < 3)
                return System.Drawing.Color.FromArgb((byte)(m * 255), (byte)((chroma + m) * 255), (byte)((x + m) * 255));
            if (hmed >= 3 && hmed < 4)
                return System.Drawing.Color.FromArgb((byte)(m * 255), (byte)((x + m) * 255), (byte)((chroma + m) * 255));
            if (hmed >= 4 && hmed < 5)
                return System.Drawing.Color.FromArgb((byte)((x + m) * 255), (byte)(m * 255), (byte)((chroma + m) * 255));
            if (hmed >= 5 && hmed <= 6)
                return System.Drawing.Color.FromArgb((byte)((chroma + m) * 255), (byte)(m * 255), (byte)((x + m) * 255));
            byte c = (byte)(m * 255);
            return System.Drawing.Color.FromArgb(c, c, c);
        }

        /**
         * <summary>
         * Returns and array of three decimals in order: hue (0 - 360), saturation (0 - 1), value (0 - 1)
         * </summary>
         **/
        private static decimal[] GetHSVFromRGB(System.Drawing.Color c)
        {
            return GetHSVFromRGB(c.R, c.G, c.B);
        }

        /**
         * <summary>
         * Returns and array of three decimals in order: hue (0 - 360), saturation (0 - 1), value (0 - 1)
         * </summary>
         **/
        private static decimal[] GetHSVFromRGB(byte r, byte g, byte b)
        {
            byte min = Math.Min(Math.Min(r, g), Math.Min(g, b));
            byte max = Math.Max(Math.Max(r, g), Math.Max(g, b));

            decimal hue = 0, saturation, value;
            if (max - min > 0)
            {
                if (r == max)
                    hue = (decimal)((double)(g - b) / (max - min)) * 60;
                else if (g == max)
                    hue = (decimal)(2 + (double)(b - r) / (max - min)) * 60;
                else
                    hue = (decimal)(4 + (double)(r - g) / (max - min)) * 60;
                saturation = (decimal)(max - min) / max;
            }
            else
                saturation = 0;

            value = (decimal) max / 255;

            if (hue < 0)
                hue += 360;

            return new decimal[] { hue, saturation, value };
        }

        private Regex regex = new Regex("([^A-Fa-f0-9])+");
        private void HTMLPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (regex.IsMatch(e.Text))
                e.Handled = true;
        }

        private void HTMLKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                HTMLLostFocus(sender, null);
        }

        public void SetRGB(int red, int green, int blue)
        {
            R.Value = red;
            G.Value = green;
            B.Value = blue;

            ChangeHue(System.Drawing.Color.FromArgb(red, green, blue));
        }

        private void HTMLLostFocus(object sender, RoutedEventArgs e)
        {
            if (HTML.Text.Length > 6)
                HTML.Text = HTML.Text.Substring(0, 6);
            else
            {
                for (int i = HTML.Text.Length; i < 6; i++)
                    HTML.Text += "0";
            }

            R.Value = byte.Parse(HTML.Text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            G.Value = byte.Parse(HTML.Text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            B.Value = byte.Parse(HTML.Text.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        }

        public System.Drawing.Color GetSelectedColor()
        {
            System.Windows.Media.Color c = (CurrentColor.Fill as SolidColorBrush).Color;
            if(c != null)
                return System.Drawing.Color.FromArgb(c.R, c.G, c.B);
            return System.Drawing.Color.Black;
        }

        public System.Windows.Media.Color GetSelectedMediaColor()
        {
            return (CurrentColor.Fill as SolidColorBrush).Color;
        }
    }

    internal struct Point
    {
        public double x, y;

        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
