using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TeacherScheduler
{
    public class SolutionsViewModel : ObservableObject, IPageViewModel
    {        
        public delegate int[,] SchoolsDistsGraphGetter();

        private int solutionDisplayedIdx = 0;
        private Solution solutionDisplayed = null;
        private bool isSolutionInProgress = false;
        private int solutionsSearchProgress;
#if DEBUG
        private Logger logger = new Logger("progress_debug.txt");
#endif
        private Teacher teacher;
        private IList<School> schools;
        private IList<Student> students;
        private SchoolsDistsGraphGetter schoolsDistsGraphGetter;
        private ObservableCollection<Solution> solutions = new ObservableCollection<Solution>();
        private SchedulingSolver schedulingSolver;

        public string Name { get; private set; } = "Solutions Browser";

        public bool IsSolutionInProgress
        {
            get
            {
                return isSolutionInProgress;
            }

            set
            {
                if (isSolutionInProgress != value)
                {
                    isSolutionInProgress = value;
                    OnPropertyChanged("IsSolutionInProgress");
                }
            }
        }

        public int SolutionDisplayedIdx
        {
            get
            {
                return solutionDisplayedIdx;
            }
            set
            {                
                solutionDisplayedIdx = value;
                if (solutionDisplayedIdx < Solutions.Count)
                    SolutionDisplayed = Solutions[solutionDisplayedIdx];
                else
                    SolutionDisplayed = null;

                OnPropertyChanged("SolutionDisplayedIdx");                
            }
        }
        
        public Solution SolutionDisplayed
        {
            get
            {
                return solutionDisplayed;
            }
            set
            {
                if (solutionDisplayed != value)
                {
                    solutionDisplayed = value;
                    OnPropertyChanged("SolutionDisplayed");
                }
            }
        }
        
        public int SolutionsSearchProgress
        {
            get
            {
                return solutionsSearchProgress;
            }
            set
            {
                if (solutionsSearchProgress != value) {
                    solutionsSearchProgress = value;
                    OnPropertyChanged("SolutionsSearchProgress");
                }
            }            
        }

        public ObservableCollection<Solution> Solutions
        {
            get
            {
                return solutions;
            }
            set
            {
                if (solutions != value)
                {
                    solutions = value;
                    OnPropertyChanged("Solutions");
                }
            }
        }

        public ICommand FindSolutions { get; }
        public ICommand StopSolutionsSearch { get; }
        public ICommand DisplayNextSolution { get; }
        public ICommand DisplayStepsForwardSolution { get; }
        public ICommand DisplayPrevSolution { get; }
        public ICommand DisplayStepsBackwardSolution { get; }
        public ICommand OnSolutionTableCellMouseDown { get; }
        public SolutionsViewModel(Teacher teacher, IList<School> schools, IList<Student> students, SchoolsDistsGraphGetter schoolsDistsGraphGetter)
        {
            this.teacher = teacher;
            this.schools = schools;
            this.students = students;
            this.schoolsDistsGraphGetter = schoolsDistsGraphGetter;            
            FindSolutions = new RelayCommand(p => findSolutions(), p => students.Count > 0 && !IsSolutionInProgress);
            StopSolutionsSearch = new RelayCommand(p => stopSolutionsSearch(), p => IsSolutionInProgress);
            OnSolutionTableCellMouseDown = new RelayCommand(p => onSolutionTableCellMouseDown(), null);
            DisplayNextSolution = new RelayCommand(p => displayNextSolution(), p => SolutionDisplayedIdx < Solutions.Count - 1);
            DisplayStepsForwardSolution = new RelayCommand(p => displayStepsForwardSolution((int)p), p => SolutionDisplayedIdx < Solutions.Count - 1);            
            DisplayPrevSolution = new RelayCommand(p => displayPrevSolution(), p => 0 < SolutionDisplayedIdx);
            DisplayStepsBackwardSolution = new RelayCommand(p => displayStepsBackwardSolution((int)p), p => 0 < SolutionDisplayedIdx);
        }

        //Teacher teacher, List<Student> students
        private void findSolutions()
        {            
            int studentsNr = students.Count;
            bool[][,] studentsSchedulesAsBoolMatrix = new bool[studentsNr][,];
            int[] studentsRequiredHours = new int[studentsNr];
            int[] studentsSchoolsIdxs = new int[studentsNr];
            for (int studentIdx = 0; studentIdx < studentsNr; studentIdx++)
            {
                studentsSchedulesAsBoolMatrix[studentIdx] = new bool[App.HOURS_NR, App.DAYS_LABELS.Length];
                loadScheduleToBoolMatrix(studentsSchedulesAsBoolMatrix[studentIdx], students[studentIdx].Schedule);
                studentsRequiredHours[studentIdx] = students[studentIdx].RequiredHoursNr;
                studentsSchoolsIdxs[studentIdx] = schools.IndexOf(students[studentIdx].SchoolAttended);
            }

            bool[,] teacherScheduleAsBoolMatrix = new bool[App.HOURS_NR, App.DAYS_LABELS.Length];
            loadScheduleToBoolMatrix(teacherScheduleAsBoolMatrix, teacher.Schedule);
#if DEBUG
            logger.clear();
#endif
            SolutionsSearchProgress = 0;
            IsSolutionInProgress = true;            
            schedulingSolver = new SchedulingSolver(studentsSchedulesAsBoolMatrix, studentsRequiredHours, studentsSchoolsIdxs, schoolsDistsGraphGetter(), teacherScheduleAsBoolMatrix);
            schedulingSolver.solve(onSolutionProgress, onSolutionDone);
        }

        private void stopSolutionsSearch()
        {
            schedulingSolver.stopSolutionsSearch();
            IsSolutionInProgress = false;
        }

        private void loadScheduleToBoolMatrix(bool[,] scheduleAsBoolMatrix, ObservableMatrix<bool> schedule)
        {            
            for (int hourIdx = 0; hourIdx < schedule.Count; hourIdx++)
            {
                for (int dayIdx = 0; dayIdx < schedule[hourIdx].Count; dayIdx++)
                    scheduleAsBoolMatrix[hourIdx, dayIdx] = schedule[hourIdx][dayIdx];
            }
        }

        private void onSolutionProgress(int progress)
        {
#if DEBUG
            logger.appendLine(progress.ToString());
#endif
            SolutionsSearchProgress = progress;
        }

        private void onSolutionDone(List<int[,]> solutionsMatricesAsInt, bool wasCanceled)
        {
#if DEBUG
            logger.write();
#endif
            if (!wasCanceled) { 
                SolutionsSearchProgress = 100;

                List<Solution> solutionsBuffer = new List<Solution>(solutionsMatricesAsInt.Count);
                foreach (int[,] solutionMatrixAsInt in solutionsMatricesAsInt)
                {
                    List<Solution.Placement> placements = new List<Solution.Placement>();
                    for (int hourIdx = 0; hourIdx < solutionMatrixAsInt.GetLength(0); hourIdx++)
                    {
                        for (int dayIdx = 0; dayIdx < solutionMatrixAsInt.GetLength(1); dayIdx++)
                        {
                            if (solutionMatrixAsInt[hourIdx, dayIdx] > 0)
                                placements.Add(new Solution.Placement { student = students[solutionMatrixAsInt[hourIdx, dayIdx] - 1], hourIdx = hourIdx, dayIdx = dayIdx });
                        }
                    }

                    solutionsBuffer.Add(new Solution(placements));
                }                
                if (solutionsBuffer.Count > 0)                
                    Solutions = new ObservableCollection<Solution>(solutionsBuffer);                                    
                else
                    Solutions = new ObservableCollection<Solution>();

                SolutionDisplayedIdx = 0;
            }
        
            IsSolutionInProgress = false;
        }

        private void onSolutionTableCellMouseDown()
        {
            
        }

        private void displayNextSolution()
        {
            SolutionDisplayedIdx++;
        }

        private void displayStepsForwardSolution(int stepsNr)
        {
            SolutionDisplayedIdx = Math.Min(SolutionDisplayedIdx + stepsNr, Solutions.Count - 1);
        }

        private void displayPrevSolution()
        {
            SolutionDisplayedIdx--;
        }

        private void displayStepsBackwardSolution(int stepsNr)
        {
            SolutionDisplayedIdx = Math.Max(SolutionDisplayedIdx - stepsNr, 0);
        }
    }
}
