using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace TeacherScheduler
{
    public static class InputBindingsManager
    {
        public static readonly DependencyProperty UpdateSourcePropertyOnEnterPressProperty = DependencyProperty.RegisterAttached(
            "UpdateSourcePropertyOnEnterPress", typeof(DependencyProperty), typeof(InputBindingsManager), new PropertyMetadata(OnUpdateSourceForEnterPress));

        public static DependencyProperty GetUpdateSourcePropertyOnEnterPress(DependencyObject lmnt)
        {
            return (DependencyProperty)lmnt.GetValue(UpdateSourcePropertyOnEnterPressProperty);
        }

        public static void SetUpdateSourcePropertyOnEnterPress(DependencyObject lmnt, DependencyProperty dp)
        {
            lmnt.SetValue(UpdateSourcePropertyOnEnterPressProperty, dp);
        }

        public static void OnUpdateSourceForEnterPress(DependencyObject lmnt, DependencyPropertyChangedEventArgs e)
        {
            UIElement uiLmnt = lmnt as UIElement;

            if (uiLmnt == null)
                return;

            if (e.OldValue != null)
                uiLmnt.PreviewKeyDown -= OnTextBoxEnterPressed;

            if (e.NewValue != null)
                uiLmnt.PreviewKeyDown += OnTextBoxEnterPressed;                            
        }

        public static void OnTextBoxEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UIElement uiLmnt = e.Source as UIElement;
                if (uiLmnt == null)
                    return;

                DependencyProperty dp = GetUpdateSourcePropertyOnEnterPress(uiLmnt);
                if (dp == null)
                    return;

                BindingExpression bindingExpression = BindingOperations.GetBindingExpression(uiLmnt, dp);
                if (bindingExpression != null)
                {
                    bindingExpression.UpdateSource();
                    Keyboard.ClearFocus();
                }
            }
        }
    }
}
