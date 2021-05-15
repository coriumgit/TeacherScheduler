using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeacherScheduler
{
    public class AppViewModel : ObservableObject
    {
        private IPageViewModel pageCurr;

        public List<IPageViewModel> Pages { get; } = new List<IPageViewModel>();

        public IPageViewModel PageCurr {
            get 
            { 
                return pageCurr; 
            } 
            
            set
            {
                if (pageCurr != value)
                {
                    pageCurr = value;
                    OnPropertyChanged("PageCurr");
                }
            }
        }        

        public ICommand ChangePage { get; }

        public AppViewModel()
        {
            TeacherViewModel teacherViewModel = new TeacherViewModel();
            Pages.Add(teacherViewModel);
            SchoolsViewModel schoolsViewModel = new SchoolsViewModel();
            Pages.Add(schoolsViewModel);
            StudentsViewModel studentsViewModel = new StudentsViewModel(schoolsViewModel.Schools);
            Pages.Add(studentsViewModel);            
            Pages.Add(new SolutionsViewModel(teacherViewModel.TheTeacher, schoolsViewModel.Schools, studentsViewModel.Students, schoolsViewModel.getSchoolsDistsGraph));

            PageCurr = Pages[0];

            ChangePage = new RelayCommand(page => changePage((IPageViewModel)page), page => page is IPageViewModel);
        }

        private void changePage(IPageViewModel pageViewModel)
        {
            PageCurr = Pages.FirstOrDefault(vm => vm == pageViewModel);
        }
    }
}
