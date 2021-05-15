using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace TeacherScheduler
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SchoolsView : UserControl
    {
        private Point mouseStartPos;
        private Border selectedSchoolRect = null;
        private int dragSrcSetIdx = -1;
        private int dropTargetSetIdx = -1;
        private bool doCreateNewSchoolsGrp = false;        
        private DragDropHightlightedCloneAdorner dragdropAdorner = null;
        private GradientHighlightAdorner itemsHolderAdorner = null;
        private bool isSchoolBeingClickedOn = false;
        private bool isDragging = false;
        private SchoolsViewModel dataContext;

        public static readonly DependencyProperty SelectedSchoolProperty = DependencyProperty.Register("SelectedSchool", typeof(School), typeof(SchoolsView));

        public School SelectedSchool
        {
            get { return (School)GetValue(SelectedSchoolProperty); }
            set { SetValue(SelectedSchoolProperty, value); }
        }

        public SchoolsView()
        {
            InitializeComponent();            
        }

        public void onLoad(object sender, RoutedEventArgs e)
        {
            dataContext = (SchoolsViewModel)DataContext;
        }

        public void onMouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedSchoolRect = (Border)e.OriginalSource;
            if (selectedSchoolRect != null && selectedSchoolRect.DataContext is School)
            {
                if (SelectedSchool != null)                
                    AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(dragdropAdorner);
                                
                isSchoolBeingClickedOn = true;                
                SelectedSchool = (School)selectedSchoolRect.DataContext;                
                dragSrcSetIdx = dropTargetSetIdx = getItemIdxInPanel((ItemsControl)sender);

                dragdropAdorner = new DragDropHightlightedCloneAdorner(selectedSchoolRect, Color.FromArgb(128, 255, 255, 255));
                AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Add(dragdropAdorner);
                dragdropAdorner.IsHitTestVisible = false;
                mouseStartPos = Mouse.GetPosition(schoolsAvatarsPanel);
            }
            else
            {
                SelectedSchool = null;
                if (dragdropAdorner != null)
                    AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(dragdropAdorner);
            }

            e.Handled = true;
        }

        public void onMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {                
                if (isSchoolBeingClickedOn && (Mouse.GetPosition(schoolsAvatarsPanel) - mouseStartPos).LengthSquared > SystemParameters.MinimumHorizontalDragDistance * SystemParameters.MinimumHorizontalDragDistance)
                {
                    isDragging = true;
                    dragdropAdorner.IsBeingDragged = true;
                    Mouse.Capture(schoolsAvatarsPanel, CaptureMode.SubTree);
                    //DataObject schoolDataObj = new DataObject();
                    //schoolDataObj.SetData("School", SelectedSchool);
                    //DragDrop.DoDragDrop((ItemsControl)sender, schoolDataObj, DragDropEffects.Move);
                    ((Border)selectedSchoolRect.Parent).Visibility = Visibility.Hidden;
                }
            }
            else
            {
                Point mousePos = Mouse.GetPosition(schoolsAvatarsPanel);
                dragdropAdorner.LeftOffset = mousePos.X - mouseStartPos.X;
                dragdropAdorner.TopOffset = mousePos.Y - mouseStartPos.Y;
            }
        }

        public void onMouseEnterSchoolsGrp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                ItemsControl itemsControl = (ItemsControl)sender;
                dropTargetSetIdx = getItemIdxInPanel((ItemsControl)sender);
                if (dragSrcSetIdx != dropTargetSetIdx)                
                    highlightSchoolsGrp(itemsControl);                                    
            }                        
        }

        public void onMouseEnterNewGrpCreationArea(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                highlightSchoolsGrp(createNewGrpArea);
                dropTargetSetIdx = schoolsGroupsList.ItemContainerGenerator.Items.Count;
            }
        }        

        private void highlightSchoolsGrp(UIElement schoolsGrpCtrl)
        {
            itemsHolderAdorner = new GradientHighlightAdorner(schoolsGrpCtrl, Color.FromRgb(0, 255, 0));
            AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(dragdropAdorner);
            AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Add(itemsHolderAdorner);
            AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Add(dragdropAdorner);
            itemsHolderAdorner.IsHitTestVisible = false;
        }

        public void onMouseLeaveSchoolsGrp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                dropTargetSetIdx = -1;
                if (itemsHolderAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(itemsHolderAdorner);
                    itemsHolderAdorner = null;
                }                
            }         
        }

        public void onMouseLeaveNewGrpCreationArea(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                dropTargetSetIdx = -1;                
                AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(itemsHolderAdorner);
                itemsHolderAdorner = null;                
            }                        
        }

        public void onMouseUp(object sender, MouseButtonEventArgs e)
        {
            isSchoolBeingClickedOn = false;
            if (isDragging)
            {
                isDragging = false;
                AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(dragdropAdorner);
                dragdropAdorner = null;

                if (dropTargetSetIdx != -1 && dropTargetSetIdx != dragSrcSetIdx)
                {                    
                    AdornerLayer.GetAdornerLayer(schoolsAvatarsPanel).Remove(itemsHolderAdorner);
                    itemsHolderAdorner = null;

                    dataContext.MoveSchoolBetweenSetsCmd.Execute(new object[2] { SelectedSchool, dropTargetSetIdx });                                        
                }
                else // dropTargetCollection == -1 || dropTargetSetIdx == dragSrcSetIdx               
                    ((Border)selectedSchoolRect.Parent).Visibility = Visibility.Visible;
                
                SelectedSchool = null;                                
                Mouse.Capture(null);
            }            
            
            e.Handled = true;            
        }      

        private int getItemIdxInPanel(DependencyObject item)
        {
            DependencyObject itemsIt = item;
            DependencyObject itemsItParent = VisualTreeHelper.GetParent(itemsIt);
            while (itemsItParent != null && !(itemsItParent is Panel))
            {
                itemsIt = VisualTreeHelper.GetParent(itemsIt);
                itemsItParent = VisualTreeHelper.GetParent(itemsIt);
            }

            if (itemsItParent != null)
                return ((Panel)itemsItParent).Children.IndexOf((UIElement)itemsIt);
            else
                return -1;
        }
    }
}
